using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BrowserSelector.Services;

namespace BrowserSelector.Pages
{
    public partial class HomePage : UserControl
    {
        public event EventHandler? NavigateToRulesRequested;
        public event EventHandler? TestAllRulesRequested;

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        public void LoadData()
        {
            LoadDefaultBrowser();
            LoadStats();
        }

        private void LoadDefaultBrowser()
        {
            try
            {
                var browsers = BrowserService.GetBrowsersWithColors();
                DefaultBrowserComboBox.ItemsSource = browsers;

                var savedBrowser = DefaultBrowserManager.Load();
                if (savedBrowser != null)
                {
                    DefaultBrowserComboBox.SelectedItem =
                        browsers.FirstOrDefault(b =>
                            b.ExecutablePath.Equals(
                                savedBrowser.ExecutablePath,
                                StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"HomePage LoadDefaultBrowser ERROR: {ex.Message}");
            }
        }

        private void LoadStats()
        {
            try
            {
                var rules = UrlRuleManager.LoadRules();
                var groups = UrlGroupManager.LoadGroups();
                var browsers = BrowserDetector.GetInstalledBrowsers();

                RulesCountText.Text = rules.Count.ToString();
                GroupsCountText.Text = groups.Count.ToString();
                BrowsersCountText.Text = browsers.Count.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log($"HomePage LoadStats ERROR: {ex.Message}");
            }
        }

        private void DefaultBrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultBrowserComboBox.SelectedItem is BrowserInfoWithColor browserWithColor)
            {
                var browser = new BrowserInfo
                {
                    Name = browserWithColor.Name,
                    ExecutablePath = browserWithColor.ExecutablePath,
                    Type = browserWithColor.Type
                };
                DefaultBrowserManager.Save(browser);
                Logger.Log($"User changed default browser to: {browser.Name}");
            }
        }

        private void ManageRules_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRulesRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TestAllRules_Click(object sender, RoutedEventArgs e)
        {
            TestAllRulesRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
