using Bloxstrap.Resources;
using Bloxstrap.UI.Elements.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Common.Interfaces;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class IntegrationsViewModel : NotifyPropertyChangedViewModel, INavigationAware
    {

        public bool VulkanFullscreenAllowed => App.Settings.Prop.EnableWindowManipulation && (App.FastFlags.GetPreset("Rendering.Mode.Vulkan") ?? "False").Equals("True", StringComparison.OrdinalIgnoreCase);

        public bool EnableRobloxBackgroundApp
        {
            get => App.Settings.Prop.EnableRobloxBackgroundApp;
            set => App.Settings.Prop.EnableRobloxBackgroundApp = value;
        }

        public IReadOnlyCollection<RobloxTheme> RobloxThemes { get; } = RobloxThemeEx.Selections;

        public RobloxTheme RobloxTheme
        {
            get => App.Settings.Prop.RobloxTheme;
            set => App.Settings.Prop.RobloxTheme = value;
        }

        public ICommand AddIntegrationCommand => new RelayCommand(AddIntegration);

        public ICommand DeleteIntegrationCommand => new RelayCommand(DeleteIntegration);

        public ICommand BrowseIntegrationLocationCommand => new RelayCommand(BrowseIntegrationLocation);

        public void OnNavigatedTo()
        {
            OnPropertyChanged(nameof(VulkanFullscreenAllowed));
            OnPropertyChanged(nameof(EnableFakeBorderlessFullscreen));
            OnPropertyChanged(nameof(SessionStatsSummary));
            OnPropertyChanged(nameof(WeeklyStats));
            OnPropertyChanged(nameof(WeeklyStatsVisibility));
        } // vulkan is updated on a different page so we do this

        public void OnNavigatedFrom() { }

        private void AddIntegration()
        {
            CustomIntegrations.Add(new CustomIntegration()
            {
                Name = Strings.Menu_Integrations_Custom_NewIntegration
            });

            SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;

            OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void DeleteIntegration()
        {
            if (SelectedCustomIntegration is null)
                return;

            CustomIntegrations.Remove(SelectedCustomIntegration);

            if (CustomIntegrations.Count > 0)
            {
                SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            }

            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void BrowseIntegrationLocation()
        {
            if (SelectedCustomIntegration is null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_AllFiles}|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            SelectedCustomIntegration.Name = dialog.SafeFileName;
            SelectedCustomIntegration.Location = dialog.FileName;
            OnPropertyChanged(nameof(SelectedCustomIntegration));
        }

        public bool ActivityTrackingEnabled
        {
            get => App.Settings.Prop.EnableActivityTracking;
            set
            {
                App.Settings.Prop.EnableActivityTracking = value;

                if (!value)
                {
                    ShowServerDetailsEnabled = value;
                    DisableAppPatchEnabled = value;
                    DiscordActivityEnabled = value;
                    DiscordActivityJoinEnabled = value;

                    OnPropertyChanged(nameof(ShowServerDetailsEnabled));
                    OnPropertyChanged(nameof(DisableAppPatchEnabled));
                    OnPropertyChanged(nameof(DiscordActivityEnabled));
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                }
            }
        }

        public string SessionStatsSummary
        {
            get
            {
                var sessions = App.SessionStats.Prop;

                if (sessions.Count == 0)
                    return Strings.Menu_Integrations_SessionStats_NoData;

                var last = sessions.MaxBy(x => x.Start);
                TimeSpan total = TimeSpan.Zero;

                foreach (var session in sessions)
                {
                    if (session.End is not null)
                        total += session.End.Value - session.Start;
                }

                return String.Format(Strings.Menu_Integrations_SessionStats_Last, $"{last.Start:G} ({last.GamesJoined})") + "\n" +
                    String.Format(Strings.Menu_Integrations_SessionStats_Total, sessions.Count) + "\n" +
                    String.Format(Strings.Menu_Integrations_SessionStats_Playtime, $"{total.TotalHours:F1}");
            }
        }

        public Visibility WeeklyStatsVisibility => App.SessionStats.Prop.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public IReadOnlyList<WeeklyStatDay> WeeklyStats
        {
            get
            {
                var days = new List<WeeklyStatDay>();

                var now = DateTime.Now;
                var weekStart = now.Date.AddDays(-6);

                var perDay = new Dictionary<DateTime, (TimeSpan Duration, int Sessions)>();

                foreach (var session in App.SessionStats.Prop)
                {
                    if (session.Start < weekStart)
                        continue;

                    var end = session.End ?? now;

                    if (end < session.Start)
                        end = session.Start;

                    var key = session.Start.Date;

                    if (!perDay.TryGetValue(key, out var value))
                        value = (TimeSpan.Zero, 0);

                    value.Duration += end - session.Start;
                    value.Sessions++;
                    perDay[key] = value;
                }

                double maxMinutes = perDay.Count == 0 ? 0 : perDay.Values.Max(x => x.Duration.TotalMinutes);

                for (int i = 0; i < 7; i++)
                {
                    var day = weekStart.AddDays(i);

                    perDay.TryGetValue(day, out var value);

                    string durationText = value.Duration.TotalHours >= 1
                        ? String.Format(Strings.Menu_Integrations_SessionStats_Week_DurationHours, (int)value.Duration.TotalHours, value.Duration.Minutes)
                        : String.Format(Strings.Menu_Integrations_SessionStats_Week_DurationMinutes, (int)Math.Round(value.Duration.TotalMinutes));

                    days.Add(new WeeklyStatDay
                    {
                        DayLabel = $"{day:ddd d.MM}",
                        DurationLabel = $"{durationText} · {String.Format(Strings.Menu_Integrations_SessionStats_Week_Sessions, value.Sessions)}",
                        BarWidth = maxMinutes > 0 && value.Duration.TotalMinutes > 0 ? Math.Max(3, value.Duration.TotalMinutes / maxMinutes * 260) : 0
                    });
                }

                return days;
            }
        }

        public bool ShowServerDetailsEnabled
        {
            get => App.Settings.Prop.ShowServerDetails;
            set => App.Settings.Prop.ShowServerDetails = value;
        }

        public bool DiscordActivityEnabled
        {
            get => App.Settings.Prop.UseDiscordRichPresence;
            set
            {
                App.Settings.Prop.UseDiscordRichPresence = value;

                if (!value)
                {
                    DiscordActivityJoinEnabled = value;
                    DiscordInvitesEnabled = value;
                    DiscordAccountOnProfile = value;
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                    OnPropertyChanged(nameof(DiscordInvitesEnabled));
                    OnPropertyChanged(nameof(DiscordAccountOnProfile));
                }
            }
        }
        public IReadOnlyList<DiscordRPCStatusDisplay> DiscordActivityStatusDisplayTypes => DiscordRPCStatusDisplayEx.Selections;

        public DiscordRPCStatusDisplay DiscordActivityStatusDisplayType
        {
            get => App.Settings.Prop.RichPresenceStatusDisplayType;
            set => App.Settings.Prop.RichPresenceStatusDisplayType = value;
        }

        public bool DiscordActivityJoinEnabled
        {
            get => !App.Settings.Prop.HideRPCButtons;
            set => App.Settings.Prop.HideRPCButtons = !value;
        }

        public bool DiscordInvitesEnabled
        {
            get => App.Settings.Prop.AllowDiscordInvites;
            set => App.Settings.Prop.AllowDiscordInvites = value;
        }

        public bool DiscordAccountOnProfile
        {
            get => App.Settings.Prop.ShowAccountOnRichPresence;
            set => App.Settings.Prop.ShowAccountOnRichPresence = value;
        }

        public bool DisableAppPatchEnabled
        {
            get => App.Settings.Prop.UseDisableAppPatch;
            set => App.Settings.Prop.UseDisableAppPatch = value;
        }

        public ObservableCollection<CustomIntegration> CustomIntegrations
        {
            get => App.Settings.Prop.CustomIntegrations;
            set => App.Settings.Prop.CustomIntegrations = value;
        }

        public bool EnableWindowManipulation
        {
            get => App.Settings.Prop.EnableWindowManipulation;
            set
            {
                App.Settings.Prop.EnableWindowManipulation = value;

                if (!value)
                    EnableFakeBorderlessFullscreen = false;

                OnPropertyChanged(nameof(VulkanFullscreenAllowed));
                OnPropertyChanged(nameof(EnableFakeBorderlessFullscreen));
            }
        }

        public bool EnableFakeBorderlessFullscreen
        {
            get => App.Settings.Prop.FakeBorderlessFullscreen;
            set => App.Settings.Prop.FakeBorderlessFullscreen = VulkanFullscreenAllowed && value;
        }

        public CustomIntegration? SelectedCustomIntegration { get; set; }
        public int SelectedCustomIntegrationIndex { get; set; }
        public bool IsCustomIntegrationSelected => SelectedCustomIntegration is not null;
    }

    public class WeeklyStatDay
    {
        public string DayLabel { get; set; } = "";

        public string DurationLabel { get; set; } = "";

        public double BarWidth { get; set; } = 0;
    }
}
