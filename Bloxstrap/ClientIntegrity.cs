namespace Bloxstrap
{
    using Bloxstrap.AppData;

    public enum ModFileStatus
    {
        Applied,
        Modified,
        SourceMissing,
        ClientMissing
    }

    public class ModFileInfo
    {
        public string RelativePath { get; set; } = "";

        public ModFileStatus Status { get; set; }
    }

    public static class ClientIntegrity
    {
        public static event EventHandler? IssuesDetectedEvent;

        /// <summary>
        /// Quietly checks the client in the background after startup and raises
        /// IssuesDetectedEvent when files were changed outside the mod system.
        /// </summary>
        public static void RunStartupCheck()
        {
            Task.Run(async () =>
            {
                const string LOG_IDENT = "ClientIntegrity::RunStartupCheck";

                try
                {
                    // let the app finish launching before doing any work
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    if (GetVersionFolder() is null)
                        return;

                    var modified = Check().Where(x => x.Status == ModFileStatus.Modified).ToList();

                    if (modified.Count == 0)
                        return;

                    App.Logger.WriteLine(LOG_IDENT, $"{modified.Count} client files were changed outside the mod system");

                    IssuesDetectedEvent?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Startup integrity check failed silently");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            });
        }

        public static string? GetVersionFolder()
        {
            try
            {
                string executablePath = new RobloxPlayerData().ExecutablePath;

                if (!File.Exists(executablePath))
                    return null;

                return Path.GetDirectoryName(executablePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Compares every file the mod manifest claims to be applied against
        /// its source in the Modifications folder.
        /// </summary>
        public static List<ModFileInfo> Check()
        {
            var results = new List<ModFileInfo>();

            string? versionFolder = GetVersionFolder();

            foreach (string relativeFile in App.RobloxState.Prop.ModManifest.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var info = new ModFileInfo { RelativePath = relativeFile };

                string versionFile = versionFolder is null ? "" : Path.Combine(versionFolder, relativeFile);
                string modFile = Path.Combine(Paths.Modifications, relativeFile);

                if (versionFolder is null || !File.Exists(versionFile))
                    info.Status = ModFileStatus.ClientMissing;
                else if (!File.Exists(modFile))
                    info.Status = ModFileStatus.SourceMissing;
                else if (MD5Hash.FromFile(versionFile) != MD5Hash.FromFile(modFile))
                    info.Status = ModFileStatus.Modified;
                else
                    info.Status = ModFileStatus.Applied;

                results.Add(info);
            }

            return results;
        }

        /// <summary>
        /// Removes every applied mod from the client and the Modifications folder.
        /// The mod manifest is kept intact on purpose: the bootstrapper then restores
        /// the original files from the downloaded Roblox packages at the next launch.
        /// Returns (removed, failed) file counts.
        /// </summary>
        public static (int Removed, int Failed) RestoreOriginals()
        {
            const string LOG_IDENT = "ClientIntegrity::RestoreOriginals";

            // enabled packs would re-apply their files otherwise
            foreach (string name in App.ModPacks.Names.ToList())
                App.ModPacks.SetEnabled(name, false);

            string? versionFolder = GetVersionFolder();

            var targets = new HashSet<string>(App.RobloxState.Prop.ModManifest, StringComparer.OrdinalIgnoreCase);

            foreach (string file in Directory.GetFiles(Paths.Modifications, "*.*", SearchOption.AllDirectories))
            {
                string relativeFile = file.Substring(Paths.Modifications.Length + 1);

                // the pack storage stays, the flag manager keeps ownership of its file
                if (relativeFile.StartsWith(@"Packs\", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (relativeFile.Equals(@"ClientSettings\ClientAppSettings.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                targets.Add(relativeFile);
            }

            int removed = 0;
            int failed = 0;

            foreach (string relativeFile in targets)
            {
                if (versionFolder is not null)
                {
                    string versionFile = Path.Combine(versionFolder, relativeFile);

                    try
                    {
                        if (File.Exists(versionFile))
                        {
                            Filesystem.AssertReadOnly(versionFile);
                            File.Delete(versionFile);
                            removed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to delete '{versionFile}'");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }

                string modFile = Path.Combine(Paths.Modifications, relativeFile);

                try
                {
                    if (File.Exists(modFile))
                    {
                        Filesystem.AssertReadOnly(modFile);
                        File.Delete(modFile);
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to delete '{modFile}'");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            return (removed, failed);
        }
    }
}
