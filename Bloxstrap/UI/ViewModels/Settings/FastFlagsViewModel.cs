using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Win32;

using Bloxstrap.Resources;
using Bloxstrap.Enums.FlagPresets;
using Bloxstrap.UI.Elements.Dialogs;
using System.Windows;
using Bloxstrap.UI.Elements.Settings.Pages;
using Wpf.Ui.Mvvm.Contracts;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        private Dictionary<string, object>? _preResetFlags;

        public event EventHandler? RequestPageReloadEvent;
        
        public event EventHandler? OpenFlagEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseFastFlagManager;
            set => App.Settings.Prop.UseFastFlagManager = value;
        }

        public IReadOnlyList<string> ProfileNames => FlagProfiles.Names;

        public string SelectedProfile
        {
            get => App.Settings.Prop.FlagProfile;
            set
            {
                App.Settings.Prop.FlagProfile = value;
                FlagProfiles.Apply(App.FastFlags, value);
                OnPropertyChanged(nameof(SelectedProfile));
                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public IReadOnlyDictionary<MSAAMode, string?> MSAALevels => FastFlagManager.MSAAModes;

        public MSAAMode SelectedMSAALevel
        {
            get => MSAALevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.MSAA")).Key;
            set => App.FastFlags.SetPreset("Rendering.MSAA", MSAALevels[value]);
        }

        public IReadOnlyDictionary<RenderingMode, string> RenderingModes => FastFlagManager.RenderingModes;

        public RenderingMode SelectedRenderingMode
        {
            get => App.FastFlags.GetPresetEnum(RenderingModes, "Rendering.Mode", "True");
            set
            {
                if (value != RenderingMode.Vulkan)
                    App.Settings.Prop.FakeBorderlessFullscreen = false; // vulkan exclusive

                App.FastFlags.SetPresetEnum("Rendering.Mode", value.ToString(), "True");
            }
        }

        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        private static readonly string[] LODLevels = { "L0", "L12", "L23", "L34" };

        public bool FRMQualityOverrideEnabled
        {
            get => App.FastFlags.GetPreset("Rendering.FRMQualityOverride") != null;
            set
            {
                if (value)
                    FRMQualityOverride = 21;
                else
                    App.FastFlags.SetPreset("Rendering.FRMQualityOverride", null);

                OnPropertyChanged(nameof(FRMQualityOverride));
                OnPropertyChanged(nameof(FRMQualityOverrideEnabled));
            }
        }

        public int FRMQualityOverride
        {
            get => int.TryParse(App.FastFlags.GetPreset("Rendering.FRMQualityOverride"), out var x) ? x : 21;
            set
            {
                App.FastFlags.SetPreset("Rendering.FRMQualityOverride", value);

                OnPropertyChanged(nameof(FRMQualityOverride));
            }
        }

        public bool MeshQualityEnabled
        {
            get => App.FastFlags.GetPreset("Geometry.MeshLOD.Static") != null;
            set
            {
                if (value)
                {
                    // we enable level 3 by default
                    MeshQuality = 3;
                }
                else
                {
                    foreach (string level in LODLevels)
                        App.FastFlags.SetPreset($"Geometry.MeshLOD.{level}", null);

                    App.FastFlags.SetPreset("Geometry.MeshLOD.Static", null);
                }

                OnPropertyChanged(nameof(MeshQualityEnabled));
            }
        }

        public int MeshQuality
        {
            get => int.TryParse(App.FastFlags.GetPreset("Geometry.MeshLOD.Static"), out var x) ? x : 0;
            set
            {
                // holy..
                int clamped = Math.Clamp(value, 0, LODLevels.Length - 1);

                for (int i = 0; i < LODLevels.Length; i++)
                {
                    int lodValue = (Math.Clamp(clamped - i, 0, 3) + 1) * 250;
                    string lodLevel = LODLevels[i];

                    App.FastFlags.SetPreset($"Geometry.MeshLOD.{lodLevel}", lodValue);
                }

                App.FastFlags.SetPreset("Geometry.MeshLOD.Static", clamped);
                OnPropertyChanged(nameof(MeshQuality));
                OnPropertyChanged(nameof(MeshQualityEnabled));
            }
        }

        public IReadOnlyList<int> HourOptions { get; } = Enumerable.Range(0, 24).ToList();

        public IReadOnlyList<string> SchedulableProfileNames => FlagProfiles.Names.Concat(App.CustomProfiles.Names).ToList();

        public bool ScheduleEnabled
        {
            get => App.Settings.Prop.ScheduleEnabled;
            set => App.Settings.Prop.ScheduleEnabled = value;
        }

        public int ScheduleStartHour
        {
            get => App.Settings.Prop.ScheduleStartHour;
            set
            {
                App.Settings.Prop.ScheduleStartHour = Math.Clamp(value, 0, 23);
                OnPropertyChanged(nameof(ScheduleStartHour));
            }
        }

        public int ScheduleEndHour
        {
            get => App.Settings.Prop.ScheduleEndHour;
            set
            {
                App.Settings.Prop.ScheduleEndHour = Math.Clamp(value, 0, 23);
                OnPropertyChanged(nameof(ScheduleEndHour));
            }
        }

        public string ScheduleProfile
        {
            get => App.Settings.Prop.ScheduleProfile;
            set => App.Settings.Prop.ScheduleProfile = value;
        }

        public IReadOnlyList<string> CustomProfileNames => App.CustomProfiles.Names;

        public string? SelectedCustomProfile { get; set; }

        public ICommand SaveProfileCommand => new RelayCommand(SaveCurrentProfile);

        public ICommand ApplyCustomProfileCommand => new RelayCommand(ApplyCustomProfile);

        public ICommand DeleteCustomProfileCommand => new RelayCommand(DeleteCustomProfile);

        public ICommand ExportCustomProfileCommand => new RelayCommand(ExportCustomProfile);

        public ICommand ImportCustomProfileCommand => new RelayCommand(ImportCustomProfile);

        private void SaveCurrentProfile()
        {
            var dialog = new SaveCustomProfileDialog();
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK || String.IsNullOrWhiteSpace(dialog.ProfileName))
                return;

            App.CustomProfiles.SaveProfile(dialog.ProfileName.Trim());
            RefreshCustomProfiles();
        }

        private void ApplyCustomProfile()
        {
            if (SelectedCustomProfile is null)
                return;

            App.CustomProfiles.Apply(SelectedCustomProfile);
            RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
        }

        private void DeleteCustomProfile()
        {
            if (SelectedCustomProfile is null)
                return;

            App.CustomProfiles.Delete(SelectedCustomProfile);
            RefreshCustomProfiles();
        }

        private void ExportCustomProfile()
        {
            if (SelectedCustomProfile is null)
                return;

            var dialog = new SaveFileDialog { Filter = $"{Strings.FileTypes_JSONFiles}|*.json", FileName = $"{SelectedCustomProfile}.json" };

            if (dialog.ShowDialog() != true)
                return;

            App.CustomProfiles.Export(SelectedCustomProfile, dialog.FileName);
        }

        private void ImportCustomProfile()
        {
            var dialog = new OpenFileDialog { Filter = $"{Strings.FileTypes_JSONFiles}|*.json" };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                App.CustomProfiles.Import(dialog.FileName);
            }
            catch (Exception)
            {
                Frontend.ShowMessageBox(Strings.Menu_FastFlags_ImportFailed, MessageBoxImage.Error);
                return;
            }

            RefreshCustomProfiles();
        }

        private void RefreshCustomProfiles()
        {
            OnPropertyChanged(nameof(CustomProfileNames));
            SelectedCustomProfile = null;
            OnPropertyChanged(nameof(SelectedCustomProfile));
        }

        public bool HardwareRecommendationVisible => App.State.Prop.HardwareProfileRecommended is not null && !App.State.Prop.HardwareProfileDismissed;

        public string HardwareRecommendationText => String.Format(
            Strings.Menu_FastFlags_HardwareRecommendation_Message,
            App.State.Prop.HardwareProfileGpuName,
            App.State.Prop.HardwareProfileRecommended
        );

        public ICommand ApplyHardwareRecommendationCommand => new RelayCommand(() =>
        {
            if (App.State.Prop.HardwareProfileRecommended is not null)
                SelectedProfile = App.State.Prop.HardwareProfileRecommended;
        });

        public ICommand DismissHardwareRecommendationCommand => new RelayCommand(() =>
        {
            App.State.Prop.HardwareProfileDismissed = true;
            OnPropertyChanged(nameof(HardwareRecommendationVisible));
        });

        public bool ResetConfiguration
        {
            get => _preResetFlags is not null;

            set
            {
                if (value)
                {
                    _preResetFlags = new(App.FastFlags.Prop);
                    App.FastFlags.Prop.Clear();
                }
                else
                {
                    App.FastFlags.Prop = _preResetFlags!;
                    _preResetFlags = null;
                }

                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
