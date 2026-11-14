using System.Text.Json;

namespace Bloxstrap
{
    public class CustomProfileManager : JsonManager<Dictionary<string, Dictionary<string, string>>>
    {
        public override string ClassName => "CustomProfiles";

        public override string LOG_IDENT_CLASS => nameof(CustomProfileManager);

        public override string FileLocation => Path.Combine(Paths.Base, "CustomProfiles.json");

        public List<string> Names => Prop.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        public void SaveProfile(string name)
        {
            Prop[name] = App.FastFlags.Prop
                .Where(x => x.Value is not null)
                .ToDictionary(x => x.Key, x => x.Value.ToString()!);

            Save();
        }

        public void Delete(string name)
        {
            if (!Prop.Remove(name))
                return;

            Save();
        }

        public void Apply(string name)
        {
            if (!Prop.TryGetValue(name, out var flags))
                return;

            foreach (var pair in flags)
                App.FastFlags.SetValue(pair.Key, pair.Value);
        }

        public void Export(string name, string location)
        {
            if (!Prop.TryGetValue(name, out var flags))
                return;

            File.WriteAllText(location, JsonSerializer.Serialize(flags, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Import(string location, string? name = null)
        {
            var flags = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(location));

            if (flags is null)
                throw new InvalidOperationException("Profile file is not a flag dictionary");

            Prop[name ?? Path.GetFileNameWithoutExtension(location)] = flags
                .Where(x => x.Value is not null)
                .ToDictionary(x => x.Key, x => x.Value!);

            Save();
        }
    }
}
