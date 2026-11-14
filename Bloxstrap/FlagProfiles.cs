namespace Bloxstrap
{
    public static class FlagProfiles
    {
        public const string DefaultProfile = "Default";

        public static readonly IReadOnlyList<string> Names = new List<string>
        {
            "Default",
            "Balanced",
            "Performance",
            "Quality"
        };

        private static readonly string[] MeshLodLevels = { "L0", "L12", "L23", "L34" };

        private static readonly string[] ManagedFlags =
        {
            "DFIntTaskSchedulerTargetFps",
            "DFIntDebugFRMQualityLevelOverride",
            "FIntDebugForceMSAASamples",
            "DFFlagDebugRenderForceTechnologyVoxel",
            "DFIntCSGLevelOfDetailSwitchingDistance",
            "DFIntCSGLevelOfDetailSwitchingDistanceL12",
            "DFIntCSGLevelOfDetailSwitchingDistanceL23",
            "DFIntCSGLevelOfDetailSwitchingDistanceL34",
            "DFIntCSGLevelOfDetailSwitchingDistanceStatic"
        };

        public static IReadOnlyDictionary<string, string?> Build(string name)
        {
            var flags = new Dictionary<string, string?>();

            switch (name)
            {
                case "Balanced":
                    flags["DFIntTaskSchedulerTargetFps"] = "144";
                    flags["DFIntDebugFRMQualityLevelOverride"] = "10";
                    SetMeshLod(flags, 1);
                    break;

                case "Performance":
                    flags["DFIntTaskSchedulerTargetFps"] = "240";
                    flags["DFIntDebugFRMQualityLevelOverride"] = "1";
                    flags["FIntDebugForceMSAASamples"] = "1";
                    flags["DFFlagDebugRenderForceTechnologyVoxel"] = "True";
                    SetMeshLod(flags, 0);
                    break;

                case "Quality":
                    flags["DFIntTaskSchedulerTargetFps"] = "240";
                    flags["DFIntDebugFRMQualityLevelOverride"] = "21";
                    flags["FIntDebugForceMSAASamples"] = "4";
                    SetMeshLod(flags, 3);
                    break;
            }

            foreach (string flag in ManagedFlags)
            {
                if (!flags.ContainsKey(flag))
                    flags[flag] = null;
            }

            return flags;
        }

        public static void Apply(FastFlagManager manager, string name)
        {
            foreach (var pair in Build(name))
                manager.SetValue(pair.Key, pair.Value);
        }

        private static void SetMeshLod(Dictionary<string, string?> flags, int level)
        {
            for (int i = 0; i < MeshLodLevels.Length; i++)
            {
                string suffix = MeshLodLevels[i] == "L0" ? "" : MeshLodLevels[i];
                int value = (Math.Clamp(level - i, 0, 3) + 1) * 250;
                flags[$"DFIntCSGLevelOfDetailSwitchingDistance{suffix}"] = value.ToString();
            }

            flags["DFIntCSGLevelOfDetailSwitchingDistanceStatic"] = level.ToString();
        }
    }
}
