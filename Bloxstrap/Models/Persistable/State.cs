using System.Windows.Forms;

namespace Bloxstrap.Models.Persistable
{
    public class State
    {
        public bool TestModeWarningShown { get; set; } = false;

        public bool IgnoreOutdatedChannel { get; set; } = false;

        public bool WatcherRunning { get; set; } = false;

        public bool PromptWebView2Install { get; set; } = true;

        public string? LastPage {  get; set; } = null!;

        public bool ForceReinstall { get; set; } = false;

        public bool HardwareDetectDone { get; set; } = false;

        public string? HardwareProfileRecommended { get; set; } = null;

        public string? HardwareProfileGpuName { get; set; } = null;

        public bool HardwareProfileDismissed { get; set; } = false;

        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The last FastFlags profile applied (built-in or custom), used for the tray menu checkmarks.
        /// Falls back to Settings.FlagProfile when unset.
        /// </summary>
        public string? ActiveProfile { get; set; } = null;

        public WindowState SettingsWindow { get; set; } = new();

        #region Deprecated properties
        /// <summary>
        /// Deprecated, use App.RobloxState.Player
        /// </summary>
        public AppState? Player { private get; set; }
        public AppState? GetDeprecatedPlayer() => Player;

        /// <summary>
        /// Deprecated, use App.RobloxState.Studio
        /// </summary>
        public AppState? Studio { private get; set; }
        public AppState? GetDeprecatedStudio() => Studio;

        /// <summary>
        /// Deprecated, use App.RobloxState.ModManifest
        /// </summary>
        public List<string>? ModManifest { private get; set; }
        public List<string>? GetDeprecatedModManifest() => ModManifest;
        #endregion
    }
}
