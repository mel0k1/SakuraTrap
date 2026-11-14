namespace Bloxstrap.UI.ViewModels.Settings
{
    using System.Windows;
    using System.Windows.Input;

    using Microsoft.Win32;

    using CommunityToolkit.Mvvm.Input;

    using Bloxstrap.UI.Elements.Dialogs;

    public class BehaviourViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand ExportBackupCommand => new RelayCommand(ExportBackup);

        public ICommand ImportBackupCommand => new RelayCommand(ImportBackup);

        private void ExportBackup()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SakuraTrap backup (*.zip)|*.zip",
                FileName = $"SakuraTrap-backup-{DateTime.Now:yyyyMMdd-HHmm}.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                ConfigBackup.Export(dialog.FileName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("BehaviourViewModel::ExportBackup", "Backup export failed");
                App.Logger.WriteException("BehaviourViewModel::ExportBackup", ex);

                Frontend.ShowMessageBox(Strings.Menu_Behaviour_Backup_Failed, MessageBoxImage.Error);
                return;
            }

            Frontend.ShowMessageBox(String.Format(Strings.Menu_Behaviour_Backup_ExportDone, dialog.FileName), MessageBoxImage.Information);
        }

        private void ImportBackup()
        {
            var dialog = new OpenFileDialog { Filter = "SakuraTrap backup (*.zip)|*.zip" };

            if (dialog.ShowDialog() != true)
                return;

            if (!ConfigBackup.IsValidBackup(dialog.FileName))
            {
                Frontend.ShowMessageBox(Strings.Menu_Behaviour_Backup_Invalid, MessageBoxImage.Error);
                return;
            }

            var result = Frontend.ShowMessageBox(
                Strings.Menu_Behaviour_Backup_ImportConfirm,
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                ConfigBackup.Import(dialog.FileName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("BehaviourViewModel::ImportBackup", "Backup import failed");
                App.Logger.WriteException("BehaviourViewModel::ImportBackup", ex);

                Frontend.ShowMessageBox(Strings.Menu_Behaviour_Backup_Failed, MessageBoxImage.Error);
                return;
            }

            Frontend.ShowMessageBox(Strings.Menu_Behaviour_Backup_ImportDone, MessageBoxImage.Information);

            ConfigBackup.RestartApp();
        }


        public BehaviourViewModel()
        {
            App.Cookies.StateChanged += (object? _, CookieState state) => CookieLoadingFailed = state != CookieState.Success && state != CookieState.Unknown;
        }

        public bool IsRobloxInstallationMissing => String.IsNullOrEmpty(App.RobloxState.Prop.Player.VersionGuid) && String.IsNullOrEmpty(App.RobloxState.Prop.Studio.VersionGuid);

        public bool CookieAccess
        {
            get => App.Settings.Prop.AllowCookieAccess;
            set
            {
                App.Settings.Prop.AllowCookieAccess = value;
                if (value)
                    Task.Run(App.Cookies.LoadCookies);

                OnPropertyChanged(nameof(CookieAccess));
            }
        }

        // guh
        private bool _cookieLoadingFailed;
        public bool CookieLoadingFailed
        {
            get => _cookieLoadingFailed;
            set
            {
                _cookieLoadingFailed = value;
                OnPropertyChanged(nameof(CookieLoadingFailed));
            }
        }

        public bool EnableBetterMatchmaking
        {
            get => App.Settings.Prop.EnableBetterMatchmaking;
            set => App.Settings.Prop.EnableBetterMatchmaking = value;
        }

        public bool EnableBetterMatchmakingRandomization
        {
            get => App.Settings.Prop.EnableBetterMatchmakingRandomization;
            set => App.Settings.Prop.EnableBetterMatchmakingRandomization = value;
        }

        public bool ConfirmLaunches
        {
            get => App.Settings.Prop.ConfirmLaunches;
            set => App.Settings.Prop.ConfirmLaunches = value;
        }

        public bool ForceRobloxLanguage
        {
            get => App.Settings.Prop.ForceRobloxLanguage;
            set => App.Settings.Prop.ForceRobloxLanguage = value;
        }

        public bool BackgroundUpdates
        {
            get => App.Settings.Prop.BackgroundUpdatesEnabled;
            set => App.Settings.Prop.BackgroundUpdatesEnabled = value;
        }

        public CleanerOptions SelectedCleanUpMode
        {
            get => App.Settings.Prop.CleanerOptions;
            set => App.Settings.Prop.CleanerOptions = value;
        }

        public IEnumerable<CleanerOptions> CleanerOptions { get; } = CleanerOptionsEx.Selections;

        public CleanerOptions CleanerOption
        {
            get => App.Settings.Prop.CleanerOptions;
            set
            {
                App.Settings.Prop.CleanerOptions = value;
            }
        }

        private List<string> CleanerItems = App.Settings.Prop.CleanerDirectories;

        public bool CleanerLogs
        {
            get => CleanerItems.Contains("RobloxLogs");
            set
            {
                if (value)
                    CleanerItems.Add("RobloxLogs");
                else
                    CleanerItems.Remove("RobloxLogs"); // should we try catch it?
            }
        }

        public bool CleanerCache
        {
            get => CleanerItems.Contains("RobloxCache");
            set
            {
                if (value)
                    CleanerItems.Add("RobloxCache");
                else
                    CleanerItems.Remove("RobloxCache");
            }
        }

        public bool CleanerStudioCache
        {
            get => CleanerItems.Contains("RobloxStudioCache");
            set
            {
                if (value)
                    CleanerItems.Add("RobloxStudioCache");
                else
                    CleanerItems.Remove("RobloxStudioCache");
            }
        }

        public bool CleanerSakuraTrap
        {
            get => CleanerItems.Contains("SakuraTrapLogs");
            set
            {
                if (value)
                    CleanerItems.Add("SakuraTrapLogs");
                else
                    CleanerItems.Remove("SakuraTrapLogs");
            }
        }
    }
}
