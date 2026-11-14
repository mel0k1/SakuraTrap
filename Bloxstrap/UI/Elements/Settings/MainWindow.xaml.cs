using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

using Bloxstrap.UI.ViewModels.Settings;
using Bloxstrap.UI.Elements.Settings.Pages;
using Bloxstrap.UI.Elements.Controls;

namespace Bloxstrap.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;

        // we should cache this
        private List<SearchBarItem>? _searchIndex;

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => SettingsSavedSnackbar.Show();
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;

            InitializeComponent();

            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            gbs.Opacity = viewModel.GBSEnabled ? 1 : 0.5;
            gbs.IsEnabled = viewModel.GBSEnabled; // binding doesnt work as expected so we are setting it in here instead

            LoadState();

            string? lastPageName = App.State.Prop.LastPage;
            Type? lastPage = lastPageName is null ? null : Type.GetType(lastPageName);

            App.RemoteData.Subscribe((object? sender, EventArgs e) => {
                RemoteDataBase Data = App.RemoteData.Prop;

                AlertBar.Visibility = Data.AlertEnabled ? Visibility.Visible : Visibility.Collapsed;
                AlertBar.Message = Data.AlertContent;
                AlertBar.Severity = Data.AlertSeverity;

                if (Data.KillFlags)
                    fastflags.PageType = typeof(FastFlagsDisabled);
            });

            UpdateChecker.UpdateAvailableEvent += UpdateChecker_UpdateAvailable;

            if (UpdateChecker.UpdateAvailable)
                ShowUpdateAvailable();

            ClientIntegrity.IssuesDetectedEvent += ClientIntegrity_IssuesDetected;

            if (lastPage != null)
                SafeNavigate(lastPage);

            RootNavigation.Navigated += OnNavigation!;

            void OnNavigation(object? sender, RoutedNavigationEventArgs e)
            {
                INavigationItem? currentPage = RootNavigation.Current;

                App.State.Prop.LastPage = currentPage?.PageType.FullName!;
            }

            // run scraper
            this.Loaded += (s, e) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    BuildSearchIndexAutomatically();
                }, System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                this.Width = _state.Width;

            if (_state.Height > 0)
                this.Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = _state.Left;
                this.Top = _state.Top;
            }
        }

        private async void SafeNavigate(Type page)
        {
            await Task.Delay(500); // same as below

            if (page == typeof(GlobalSettingsPage) && !App.GlobalSettings.Loaded)
                return; // prevent from navigating onto disabled page

            Navigate(page);
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        #region Update notice
        private void UpdateChecker_UpdateAvailable(object? sender, EventArgs e) => Dispatcher.Invoke(ShowUpdateAvailable);

        private void ShowUpdateAvailable()
        {
            UpdateBar.Title = Strings.Menu_Update_Title;
            UpdateBar.Message = String.Format(Strings.Menu_Update_Message, UpdateChecker.NewVersion);
            UpdateBar.Visibility = Visibility.Visible;
        }

        private bool _isUpdateDownloading = false;

        private async void UpdateBar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // ignore clicks on the InfoBar's own close button
            if (e.OriginalSource is System.Windows.Controls.Button)
                return;

            if (_isUpdateDownloading || !UpdateChecker.UpdateAvailable)
                return;

            var result = Frontend.ShowMessageBox(
                String.Format(Strings.Menu_Update_Confirm, UpdateChecker.NewVersion),
                MessageBoxImage.Question,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            _isUpdateDownloading = true;

            var progress = new Progress<int>(percent =>
                UpdateBar.Message = String.Format(Strings.Menu_Update_Downloading, UpdateChecker.NewVersion, percent)
            );

            try
            {
                await UpdateChecker.DownloadAndInstallAsync(progress);

                // the freshly downloaded installer takes over from here
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("MainWindow::UpdateBar_Click", "Update download failed");
                App.Logger.WriteException("MainWindow::UpdateBar_Click", ex);

                _isUpdateDownloading = false;
                UpdateBar.Message = String.Format(Strings.Menu_Update_Message, UpdateChecker.NewVersion);

                Frontend.ShowMessageBox(Strings.Menu_Update_DownloadFailed, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Integrity notice
        private void ClientIntegrity_IssuesDetected(object? sender, EventArgs e) => Dispatcher.Invoke(ShowIntegrityIssues);

        private void ShowIntegrityIssues()
        {
            IntegrityBar.Title = Strings.Menu_Mods_Integrity_InfoBar_Title;
            IntegrityBar.Message = Strings.Menu_Mods_Integrity_InfoBar_Message;
            IntegrityBar.Visibility = Visibility.Visible;
        }

        private void IntegrityBar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.Button)
                return;

            IntegrityBar.Visibility = Visibility.Collapsed;

            Navigate(typeof(Bloxstrap.UI.Elements.Settings.Pages.ModsPage));
        }
        #endregion

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            _state.Width = this.Width;
            _state.Height = this.Height;

            _state.Top = this.Top;
            _state.Left = this.Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            UpdateChecker.UpdateAvailableEvent -= UpdateChecker_UpdateAvailable;

            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }

        private void BuildSearchIndexAutomatically()
        {
            _searchIndex = new List<SearchBarItem>();

            var navItems = RootNavigation.Items.OfType<NavigationItem>();

            foreach (var item in navItems)
            {
                if (item.PageType == null)
                    continue;

                if (Activator.CreateInstance(item.PageType) is Page pageInstance)
                {
                    var optionControls = FindLogicalChildren<OptionControl>(pageInstance);

                    foreach (var optionControl in optionControls)
                    {
                        if (optionControl.Header is string headerText && !string.IsNullOrWhiteSpace(headerText))
                        {
                            _searchIndex.Add(new SearchBarItem
                            {
                                DisplayName = headerText,
                                PageType = item.PageType
                            });
                        }
                    }
                }
            }
        }

        private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield break;

            foreach (object rawChild in LogicalTreeHelper.GetChildren(depObj))
            {
                if (rawChild is DependencyObject child)
                {
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindLogicalChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // should move to viewmodels but uhhh im kinda lazy
        private void AutoSuggestBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is AutoSuggestBox autoSuggestBox)
            {
                var currentText = autoSuggestBox.Text;

                if (string.IsNullOrWhiteSpace(currentText))
                {
                    autoSuggestBox.ItemsSource = null;
                    return;
                }

                var selectedSetting = _searchIndex?.FirstOrDefault(x => x.DisplayName.Equals(currentText, StringComparison.OrdinalIgnoreCase));

                if (selectedSetting is not null)
                {
                    Navigate(selectedSetting.PageType);
                    return;
                }

                var query = currentText.ToLower();
                autoSuggestBox.ItemsSource = _searchIndex?
                    .Where(x => x.DisplayName.ToLower().Contains(query))
                    .Select(x => x.DisplayName)
                    .ToList();
            }
        }
    }
}