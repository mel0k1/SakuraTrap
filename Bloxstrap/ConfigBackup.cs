namespace Bloxstrap
{
    using System.IO.Compression;
    using System.Windows;

    public static class ConfigBackup
    {
        private const string LOG_IDENT = "ConfigBackup";

        // configuration files stored in the base folder
        private static readonly string[] BaseFiles =
        {
            "Settings.json",
            "State.json",
            "CustomProfiles.json",
            "ModPacks.json",
            "Sessions.json"
        };

        private static string StagingDir => Path.Combine(Paths.Temp, "config-backup");

        /// <summary>
        /// Packs the whole launcher configuration (settings, flag profiles, mod packs,
        /// custom themes and session stats) into a single zip file.
        /// Cookies and Roblox files are deliberately not included.
        /// </summary>
        public static void Export(string zipLocation)
        {
            if (Directory.Exists(StagingDir))
                Directory.Delete(StagingDir, true);

            try
            {
                Directory.CreateDirectory(StagingDir);

                foreach (string file in BaseFiles)
                {
                    string source = Path.Combine(Paths.Base, file);

                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(StagingDir, file));
                }

                // the flag manager keeps its file inside the Modifications folder
                string flagsFile = Path.Combine(Paths.Modifications, "ClientSettings\\ClientAppSettings.json");

                if (File.Exists(flagsFile))
                {
                    Directory.CreateDirectory(Path.Combine(StagingDir, "ClientSettings"));
                    File.Copy(flagsFile, Path.Combine(StagingDir, "ClientSettings", "ClientAppSettings.json"));
                }

                CopyFolder(Paths.SavedFlagProfiles, Path.Combine(StagingDir, "SavedFlagProfiles"));
                CopyFolder(Paths.CustomThemes, Path.Combine(StagingDir, "CustomThemes"));
                CopyFolder(ModPackManager.PacksFolder, Path.Combine(StagingDir, "Packs"));

                App.Logger.WriteLine(LOG_IDENT, $"Exporting configuration to {zipLocation}");

                ZipFile.CreateFromDirectory(StagingDir, zipLocation);
            }
            finally
            {
                if (Directory.Exists(StagingDir))
                    Directory.Delete(StagingDir, true);
            }
        }

        /// <summary>
        /// Quick sanity check that the zip actually is a SakuraTrap backup.
        /// </summary>
        public static bool IsValidBackup(string zipLocation)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipLocation);

                return archive.Entries.Any(x =>
                    x.FullName == "Settings.json" ||
                    x.FullName == "CustomProfiles.json" ||
                    x.FullName == "ModPacks.json"
                );
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        /// <summary>
        /// Restores the configuration from a backup zip. The app should be
        /// restarted right afterwards so the new files get loaded.
        /// </summary>
        public static void Import(string zipLocation)
        {
            if (Directory.Exists(StagingDir))
                Directory.Delete(StagingDir, true);

            try
            {
                Directory.CreateDirectory(StagingDir);

                ZipFile.ExtractToDirectory(zipLocation, StagingDir);

                App.Logger.WriteLine(LOG_IDENT, $"Importing configuration from {zipLocation}");

                foreach (string file in BaseFiles)
                {
                    string source = Path.Combine(StagingDir, file);

                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(Paths.Base, file), true);
                }

                RestoreFolder(Path.Combine(StagingDir, "ClientSettings"), Path.Combine(Paths.Modifications, "ClientSettings"));
                RestoreFolder(Path.Combine(StagingDir, "SavedFlagProfiles"), Paths.SavedFlagProfiles);
                RestoreFolder(Path.Combine(StagingDir, "CustomThemes"), Paths.CustomThemes);
                RestoreFolder(Path.Combine(StagingDir, "Packs"), ModPackManager.PacksFolder);
            }
            finally
            {
                if (Directory.Exists(StagingDir))
                    Directory.Delete(StagingDir, true);
            }
        }

        /// <summary>
        /// Starts a fresh instance of the launcher and closes the current one.
        /// </summary>
        public static void RestartApp()
        {
            App.Logger.WriteLine(LOG_IDENT, "Restarting the app");

            Process.Start(new ProcessStartInfo(Paths.Process) { UseShellExecute = true });

            Application.Current.Shutdown();
        }

        private static void CopyFolder(string source, string target)
        {
            if (!Directory.Exists(source))
                return;

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativeFile = file.Substring(source.Length).TrimStart('\\', '/');
                string fileTarget = Path.Combine(target, relativeFile);

                Directory.CreateDirectory(Path.GetDirectoryName(fileTarget)!);
                File.Copy(file, fileTarget, true);
            }
        }

        private static void RestoreFolder(string source, string target)
        {
            if (!Directory.Exists(source))
                return;

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativeFile = file.Substring(source.Length).TrimStart('\\', '/');
                string fileTarget = Path.Combine(target, relativeFile);

                Directory.CreateDirectory(Path.GetDirectoryName(fileTarget)!);
                File.Copy(file, fileTarget, true);
            }
        }
    }
}
