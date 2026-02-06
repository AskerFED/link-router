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
        public SettingsWindow()
        {
            InitializeComponent();

            // Set window size to 70% of screen
            Width = SystemParameters.PrimaryScreenWidth * 0.7;
            Height = SystemParameters.PrimaryScreenHeight * 0.7;
            MinWidth = 900;
            MinHeight = 600;

            // Auto-import built-in groups if they don't exist (disabled by default)
            UrlGroupManager.EnsureBuiltInGroupsExist();

            // Wire up events from pages
            HomePageControl.NavigateToRulesRequested += (s, e) => NavigateToPage("Rules");
            HomePageControl.TestAllRulesRequested += (s, e) => OpenTestWindow();
            RulesPageControl.DataChanged += (s, e) => UpdateRulesCount();

            LoadAllData();
            UpdateNavigationStatus();

            // Restore last selected page from settings
            var settings = SettingsManager.LoadSettings();
            NavigateToPage(settings.LastSelectedPage ?? "Home");
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
                bool isRegistered = RegistryHelper.IsRegistered();
                bool isDefault = RegistryHelper.IsSystemDefaultBrowser();

                if (!isRegistered)
                {
                    // Red - Not Registered
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(196, 43, 28));
                    StatusText.Text = "Not Registered";
                    StatusIndicatorBorder.Background = new SolidColorBrush(Color.FromRgb(253, 231, 233));
                }
                else if (!isDefault)
                {
                    // Orange - Registered but Not Default
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(202, 133, 0));
                    StatusText.Text = "Not Default";
                    StatusIndicatorBorder.Background = new SolidColorBrush(Color.FromRgb(255, 244, 206));
                }
                else
                {
                    // Green - Active as Default
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                    StatusText.Text = "Active";
                    StatusIndicatorBorder.Background = new SolidColorBrush(Color.FromRgb(223, 246, 221));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"UpdateNavigationStatus ERROR: {ex.Message}");
            }
        }

        #endregion
    }
}
