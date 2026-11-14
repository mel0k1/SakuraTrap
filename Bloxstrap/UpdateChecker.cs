using System.Text.Json;

namespace Bloxstrap
{
    public static class UpdateChecker
    {
        public const string ReleasePageUrl = "https://github.com/mel0k1/SakuraTrap/releases/latest";

        private const string ApiUrl = "https://api.github.com/repos/mel0k1/SakuraTrap/releases/latest";

        private const int CheckIntervalHours = 24;

        public static string? NewVersion { get; private set; }

        public static string? AssetUrl { get; private set; }

        public static long AssetSize { get; private set; }

        public static bool UpdateAvailable => NewVersion is not null;

        public static event EventHandler? UpdateAvailableEvent;

        public static async void CheckDaily()
        {
            const string LOG_IDENT = "UpdateChecker::CheckDaily";

            try
            {
                DateTime lastCheck = App.State.Prop.LastUpdateCheck;

                if (DateTime.Now - lastCheck < TimeSpan.FromHours(CheckIntervalHours))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Last check was {lastCheck:G}, skipping");
                    return;
                }

                App.Logger.WriteLine(LOG_IDENT, "Checking GitHub releases for a newer version");

                var response = await App.HttpClient.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();

                var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

                string? tagName = root.GetProperty("tag_name").GetString();

                string? assetUrl = null;
                long assetSize = 0;

                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        if (asset.GetProperty("name").GetString() == $"{App.ProjectName}.exe")
                        {
                            assetUrl = asset.GetProperty("browser_download_url").GetString();
                            assetSize = asset.GetProperty("size").GetInt64();
                            break;
                        }
                    }
                }

                App.State.Prop.LastUpdateCheck = DateTime.Now;
                App.State.Save();

                if (String.IsNullOrEmpty(tagName))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Latest release has no tag name, ignoring");
                    return;
                }

                string version = tagName.TrimStart('v', 'V');

                if (Utilities.CompareVersions(version, App.Version) != VersionComparison.GreaterThan)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Latest release is v{version}, current build is v{App.Version}, no update available");
                    return;
                }

                App.Logger.WriteLine(LOG_IDENT, $"Update available: v{version} (current build is v{App.Version})");

                NewVersion = version;
                AssetUrl = assetUrl;
                AssetSize = assetSize;
                UpdateAvailableEvent?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Update check failed silently");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        /// <summary>
        /// Downloads the release executable and launches it in upgrade mode.
        /// The current instance shuts down right afterwards.
        /// </summary>
        public static async Task DownloadAndInstallAsync(IProgress<int> progress)
        {
            const string LOG_IDENT = "UpdateChecker::DownloadAndInstallAsync";

            if (String.IsNullOrEmpty(AssetUrl))
                throw new InvalidOperationException("No update asset is available");

            Directory.CreateDirectory(Paths.TempUpdates);

            string downloadLocation = Path.Combine(Paths.TempUpdates, "update.exe");

            // stale file from an aborted attempt
            if (File.Exists(downloadLocation))
                File.Delete(downloadLocation);

            App.Logger.WriteLine(LOG_IDENT, $"Downloading {AssetUrl} to {downloadLocation}");

            using var response = await App.HttpClient.GetAsync(AssetUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalSize = response.Content.Headers.ContentLength ?? AssetSize;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(downloadLocation, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            var buffer = new byte[81920];
            long written = 0;
            int lastPercent = -1;
            int read;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                written += read;

                if (totalSize > 0)
                {
                    int percent = (int)(written * 100 / totalSize);

                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        progress.Report(percent);
                    }
                }
            }

            if (AssetSize > 0 && written != AssetSize)
                throw new InvalidDataException($"Downloaded file is {written} bytes, expected {AssetSize}");

            App.Logger.WriteLine(LOG_IDENT, $"Downloaded {written} bytes, launching installer in upgrade mode");

            Process.Start(new ProcessStartInfo(downloadLocation) { Arguments = "--upgrade", UseShellExecute = true });
        }
    }
}
