namespace Bloxstrap
{
    public class ModPack
    {
        public bool Enabled { get; set; } = false;

        public List<string> Files { get; set; } = new();

        public DateTime Created { get; set; } = DateTime.Now;
    }

    public class ModPackManager : JsonManager<Dictionary<string, ModPack>>
    {
        public override string ClassName => "ModPacks";

        public override string LOG_IDENT_CLASS => nameof(ModPackManager);

        public override string FileLocation => Path.Combine(Paths.Base, "ModPacks.json");

        public static string PacksFolder => Path.Combine(Paths.Modifications, "Packs");

        private static string FlagsFile => @"ClientSettings\ClientAppSettings.json";

        public IEnumerable<string> Names => Prop.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        public string GetPackFolder(string name) => Path.Combine(PacksFolder, name);

        /// <summary>
        /// Moves the loose mod files in the Modifications folder into a new pack.
        /// Returns the number of files moved, or 0 if there was nothing to move (no pack is created).
        /// </summary>
        public int CreateFromCurrentMods(string name)
        {
            const string LOG_IDENT = "ModPackManager::CreateFromCurrentMods";

            if (Prop.ContainsKey(name))
                throw new InvalidOperationException($"Pack '{name}' already exists");

            string packFolder = GetPackFolder(name);
            var files = new List<string>();

            foreach (string file in Directory.GetFiles(Paths.Modifications, "*.*", SearchOption.AllDirectories))
            {
                string relativeFile = file.Substring(Paths.Modifications.Length + 1);

                // the pack storage itself and the flag manager's file never belong to a pack
                if (relativeFile.StartsWith(@"Packs\", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (relativeFile.Equals(FlagsFile, StringComparison.OrdinalIgnoreCase))
                    continue;

                string target = Path.Combine(packFolder, relativeFile);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                Filesystem.AssertReadOnly(file);
                File.Move(file, target, true);

                files.Add(relativeFile);
            }

            if (files.Count == 0)
                return 0;

            // clean up the directories left behind by the moved files
            foreach (string directory in Directory.GetDirectories(Paths.Modifications, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length))
            {
                if (directory.StartsWith(PacksFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }

            Prop[name] = new ModPack { Files = files };
            Save();

            App.Logger.WriteLine(LOG_IDENT, $"Created pack '{name}' from {files.Count} files");

            return files.Count;
        }

        public void SetEnabled(string name, bool enabled)
        {
            if (!Prop.TryGetValue(name, out var pack) || pack.Enabled == enabled)
                return;

            if (enabled)
                Enable(name, pack);
            else
                Disable(name, pack);

            pack.Enabled = enabled;
            Save();
        }

        private void Enable(string name, ModPack pack)
        {
            const string LOG_IDENT = "ModPackManager::Enable";

            string packFolder = GetPackFolder(name);

            foreach (string relativeFile in pack.Files)
            {
                string source = Path.Combine(packFolder, relativeFile);
                string target = Path.Combine(Paths.Modifications, relativeFile);

                if (!File.Exists(source))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Pack file '{relativeFile}' is missing from storage, skipping");
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                Filesystem.AssertReadOnly(target);
                File.Copy(source, target, true);
            }

            App.Logger.WriteLine(LOG_IDENT, $"Enabled pack '{name}' ({pack.Files.Count} files)");
        }

        private void Disable(string name, ModPack pack)
        {
            const string LOG_IDENT = "ModPackManager::Disable";

            // keep files that another enabled pack is also providing
            var kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in Prop.Where(x => x.Key != name && x.Value.Enabled))
                kept.UnionWith(pair.Value.Files);

            foreach (string relativeFile in pack.Files)
            {
                if (kept.Contains(relativeFile))
                    continue;

                string target = Path.Combine(Paths.Modifications, relativeFile);

                if (!File.Exists(target))
                    continue;

                Filesystem.AssertReadOnly(target);
                File.Delete(target);
            }

            PruneEmptyDirectories();

            App.Logger.WriteLine(LOG_IDENT, $"Disabled pack '{name}' ({pack.Files.Count} files)");
        }

        public void Delete(string name)
        {
            if (!Prop.TryGetValue(name, out var pack))
                return;

            if (pack.Enabled)
                Disable(name, pack);

            if (Directory.Exists(GetPackFolder(name)))
                Directory.Delete(GetPackFolder(name), true);

            Prop.Remove(name);
            Save();
        }

        private static void PruneEmptyDirectories()
        {
            foreach (string directory in Directory.GetDirectories(Paths.Modifications, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length))
            {
                if (directory.StartsWith(PacksFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
        }
    }
}
