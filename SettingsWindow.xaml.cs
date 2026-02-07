using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrowserSelector
{
    /// <summary>
    /// Settings window with Windows 11 style left navigation
    /// Pages: Home, Manage Rules, Settings, Documentation
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private EventHandler? _navigateToRulesHandler;
        private EventHandler? _testAllRulesHandler;
        private EventHandler? _dataChangedHandler;
        private EventHandler? _dataImportedHandler;

        public SettingsWindow()
        {
            InitializeComponent();

            // Set window size to 70% of screen
            Width = SystemParameters.PrimaryScreenWidth * 0.7;
            Height = SystemParameters.PrimaryScreenHeight * 0.7;
            MinWidth = 900;
            MinHeight = 600;

            // Hide dev-only features in production
            if (!AppConfig.DevMode)
            {
                DocsNavItem.Visibility = Visibility.Collapsed;
                TestButton.Visibility = Visibility.Collapsed;
            }

            // Auto-import built-in groups if they don't exist (disabled by default)
            UrlGroupManager.EnsureBuiltInGroupsExist();

            // Wire up events from pages
            _navigateToRulesHandler = (s, e) => NavigateToPage("Rules");
            _testAllRulesHandler = (s, e) => OpenTestWindow();
            _dataChangedHandler = (s, e) => UpdateRulesCount();
            _dataImportedHandler = (s, e) => OnDataImported();

            HomePageControl.NavigateToRulesRequested += _navigateToRulesHandler;
            HomePageControl.TestAllRulesRequested += _testAllRulesHandler;
            RulesPageControl.DataChanged += _dataChangedHandler;
            SettingsPageControl.DataImported += _dataImportedHandler;

            Closing += SettingsWindow_Closing;

            LoadAllData();
            UpdateNavigationStatus();

            // Restore last selected page from settings
            var settings = SettingsManager.LoadSettings();
            var lastPage = settings.LastSelectedPage ?? "Home";

            // If Docs was last page but DevMode is off, redirect to Home
            if (lastPage == "Docs" && !AppConfig.DevMode)
            {
                lastPage = "Home";
            }

            NavigateToPage(lastPage);
        }

        private void LoadAllData()
        {
            UpdateRulesCount();
        }

        private void UpdateRulesCount()
        {
            try
            {
                var rules = UrlRuleManager.LoadRules();
                var urlGroups = UrlGroupManager.LoadGroups();

                int totalCount = rules.Count + urlGroups.Count;
                RulesCountBadge.Text = totalCount > 0 ? $"({totalCount})" : "";

                // Refresh home page stats if visible
                if (HomePageControl.Visibility == Visibility.Visible)
                {
                    HomePageControl.LoadData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"UpdateRulesCount ERROR: {ex.Message}");
            }
        }

        private void OnDataImported()
        {
            // Refresh navigation count
            UpdateRulesCount();

            // Refresh rules page if it exists
            RulesPageControl.LoadData();

            Logger.Log("Data imported - refreshed navigation counts and pages");
        }

        /// <summary>
        /// Selects the Settings page programmatically (called after registration)
        /// </summary>
        public void SelectSettingsPage()
        {
            NavigateToPage("Settings");
        }

        /// <summary>
        /// Legacy method for backwards compatibility - redirects to settings page
        /// </summary>
        public void SelectAppTab()
        {
            SelectSettingsPage();
        }

        #region Navigation

        private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavigationList.SelectedItem is ListBoxItem selectedItem)
            {
                string? tag = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    NavigateToPage(tag);
                }
            }
        }

        private void NavigateToPage(string pageName)
        {
            // Guard against calls before pages are initialized
            if (HomePageControl == null || RulesPageControl == null ||
                SettingsPageControl == null || DocsPageControl == null)
            {
                return;
            }

            // Hide all pages
            HomePageControl.Visibility = Visibility.Collapsed;
            RulesPageControl.Visibility = Visibility.Collapsed;
            SettingsPageControl.Visibility = Visibility.Collapsed;
            DocsPageControl.Visibility = Visibility.Collapsed;

            // Show selected page and update navigation selection
            switch (pageName)
            {
                case "Home":
                    HomePageControl.Visibility = Visibility.Visible;
                    HomePageControl.LoadData();
                    NavigationList.SelectedItem = HomeNavItem;
                    break;

                case "Rules":
                    RulesPageControl.Visibility = Visibility.Visible;
                    RulesPageControl.LoadData();
                    NavigationList.SelectedItem = RulesNavItem;
                    break;

                case "Settings":
                    SettingsPageControl.Visibility = Visibility.Visible;
                    SettingsPageControl.LoadData();
                    NavigationList.SelectedItem = SettingsNavItem;
                    UpdateNavigationStatus();
                    break;

                case "Docs":
                    DocsPageControl.Visibility = Visibility.Visible;
                    NavigationList.SelectedItem = DocsNavItem;
                    break;
            }

            // Persist the selected page
            try
            {
                var settings = SettingsManager.LoadSettings();
                settings.LastSelectedPage = pageName;
                SettingsManager.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save last selected page: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method to navigate to the Rules page (used by NotificationHelper)
        /// </summary>
        public void NavigateToRules()
        {
            NavigateToPage("Rules");
        }

        #endregion

        #region Test Automation

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTestWindow();
        }

        private void OpenTestWindow()
        {
            try
            {
                var testWindow = new TestAutomationWindow();
                testWindow.Owner = this;
                testWindow.ShowDialog();

                // Reload data after tests
                UpdateRulesCount();

                // Refresh current page
                if (RulesPageControl.Visibility == Visibility.Visible)
                {
                    RulesPageControl.LoadData();
                }
                else if (HomePageControl.Visibility == Visibility.Visible)
                {
                    HomePageControl.LoadData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"OpenTestWindow ERROR: {ex.Message}");
                MessageBox.Show($"Error opening Test window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Status Indicator

        public void UpdateNavigationStatus()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                bool isEnabled = settings.IsEnabled;

                if (isEnabled)
                {
                    // Green - Rules processing enabled
                    var greenColor = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                    StatusDot.Fill = greenColor;
                    StatusText.Text = "Active";
                    StatusText.Foreground = greenColor;
                    StatusIndicatorBorder.Background = new SolidColorBrush(Color.FromRgb(223, 246, 221));
                    StatusIndicatorBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(165, 214, 167));
                }
                else
                {
                    // Gray - Rules processing disabled
                    var grayColor = new SolidColorBrush(Color.FromRgb(93, 93, 93));
                    StatusDot.Fill = grayColor;
                    StatusText.Text = "Paused";
                    StatusText.Foreground = grayColor;
                    StatusIndicatorBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    StatusIndicatorBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"UpdateNavigationStatus ERROR: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        private void SettingsWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Unsubscribe from page events to prevent memory leaks
            if (_navigateToRulesHandler != null)
                HomePageControl.NavigateToRulesRequested -= _navigateToRulesHandler;
            if (_testAllRulesHandler != null)
                HomePageControl.TestAllRulesRequested -= _testAllRulesHandler;
            if (_dataChangedHandler != null)
                RulesPageControl.DataChanged -= _dataChangedHandler;
            if (_dataImportedHandler != null)
                SettingsPageControl.DataImported -= _dataImportedHandler;
        }

        #endregion
    }
}
