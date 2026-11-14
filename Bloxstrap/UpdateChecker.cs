using System.Text.Json;

namespace Bloxstrap
{
    public static class UpdateChecker
    {
        public const string ReleasePageUrl = "https://github.com/mel0k1/SakuraTrap/releases/latest";

        private const string ApiUrl = "https://api.github.com/repos/mel0k1/SakuraTrap/releases/latest";

        private const int CheckIntervalHours = 24;

        public static string? NewVersion { get; private set; }

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

                string? tagName = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("tag_name").GetString();

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
                UpdateAvailableEvent?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Update check failed silently");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
