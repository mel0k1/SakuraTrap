using System.Management;

namespace Bloxstrap
{
    public static class HardwareProfileDetector
    {
        public static void RunIfPending()
        {
            var state = App.State.Prop;

            if (state.HardwareDetectDone || App.LaunchSettings.WatcherFlag.Active || App.LaunchSettings.UninstallFlag.Active)
                return;

            Task.Run(() =>
            {
                const string LOG_IDENT = "HardwareProfileDetector";

                try
                {
                    string gpuName = Query("Win32_VideoController", "Name") ?? "";
                    uint cores = uint.TryParse(Query("Win32_Processor", "NumberOfCores"), out uint value) ? value : 0;

                    App.Logger.WriteLine(LOG_IDENT, $"Detected GPU '{gpuName}' with {cores} CPU cores");

                    state.HardwareProfileGpuName = gpuName;
                    state.HardwareProfileRecommended = Recommend(gpuName, cores);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Hardware detection failed");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
                finally
                {
                    state.HardwareDetectDone = true;
                    App.State.Save();
                }
            });
        }

        private static string? Query(string wmiClass, string property)
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");

            foreach (var item in searcher.Get())
            {
                object? value = item[property];

                if (value is not null && !String.IsNullOrEmpty(value.ToString()))
                    return value.ToString();
            }

            return null;
        }

        public static string Recommend(string gpuName, uint cpuCores)
        {
            string gpu = gpuName.ToLowerInvariant();
            int score = 0;

            if (gpu.Contains("rtx 30") || gpu.Contains("rtx 40") || gpu.Contains("rtx 50") || gpu.Contains("rx 6") || gpu.Contains("rx 7") || gpu.Contains("arc a7"))
                score += 3;
            else if (gpu.Contains("rtx 20") || gpu.Contains("gtx 16") || gpu.Contains("gtx 10") || gpu.Contains("rx 5") || gpu.Contains("arc"))
                score += 2;

            if (cpuCores >= 16)
                score += 2;
            else if (cpuCores >= 8)
                score += 1;

            if (score >= 3)
                return "Quality";

            if (score >= 2)
                return "Balanced";

            return "Performance";
        }
    }
}
