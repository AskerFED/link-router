using System;
using System.Windows;
using System.Windows.Controls;

namespace BrowserSelector
{
    public partial class EditRuleWindow : Window
    {
        private UrlRule _rule;

        public EditRuleWindow(UrlRule rule)
        {
            InitializeComponent();
            _rule = rule;
            LoadData();
        }

        private void LoadData()
        {
            PatternTextBox.Text = _rule.Pattern;

            // Load browsers
            var browsers = BrowserDetector.GetInstalledBrowsers();
            BrowserComboBox.ItemsSource = browsers;

            // Select the current browser
            foreach (BrowserInfo browser in BrowserComboBox.Items)
            {
                if (browser.Name == _rule.BrowserName)
                {
                    BrowserComboBox.SelectedItem = browser;
                    break;
                }
            }
        }

        private void BrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem is BrowserInfo browser)
            {
                var profiles = BrowserDetector.GetBrowserProfiles(browser);
                ProfileComboBox.ItemsSource = profiles;

                // Try to select the current profile
                foreach (ProfileInfo profile in ProfileComboBox.Items)
                {
                    if (profile.Name == _rule.ProfileName)
                    {
                        ProfileComboBox.SelectedItem = profile;
                        break;
                    }
                }

                // If no match, select first
                if (ProfileComboBox.SelectedItem == null && profiles.Count > 0)
                {
                    ProfileComboBox.SelectedIndex = 0;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PatternTextBox.Text))
            {
                MessageBox.Show("Please enter a URL pattern.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BrowserComboBox.SelectedItem is BrowserInfo browser &&
                ProfileComboBox.SelectedItem is ProfileInfo profile)
            {
                _rule.Pattern = PatternTextBox.Text.Trim();
                _rule.BrowserName = browser.Name;
                _rule.BrowserPath = browser.ExecutablePath;
                _rule.BrowserType = browser.Type;
                _rule.ProfileName = profile.Name;
                _rule.ProfilePath = profile.Path;
                _rule.ProfileArguments = profile.Arguments;

                try
                {
                    UrlRuleManager.UpdateRule(_rule);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving rule: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a browser and profile.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}