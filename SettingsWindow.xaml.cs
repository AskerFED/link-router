using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace BrowserSelector
{
    /// <summary>
    /// Settings window with 2 tabs: URL (rules + groups) and App (settings)
    /// Features: Dropdown add button, accordion-style groups, tab counts, test automation
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // Auto-import built-in groups if they don't exist (disabled by default)
            UrlGroupManager.EnsureBuiltInGroupsExist();

            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadRules();
            LoadUrlGroups();
            LoadAppSettings();
            UpdateTabCounts();
        }

        /// <summary>
        /// Selects the App tab programmatically (called after registration)
        /// </summary>
        public void SelectAppTab()
        {
            AppTab.IsChecked = true;
        }

        private void UpdateTabCounts()
        {
            var rules = UrlRuleManager.LoadRules();
            var urlGroups = UrlGroupManager.LoadGroups();

            int urlCount = rules.Count + urlGroups.Count;

            UrlTabCount.Text = $" ({urlCount})";
        }

        #region Tab Navigation

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (UrlPanel == null || AppPanel == null)
                return;

            UrlPanel.Visibility = UrlTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            AppPanel.Visibility = AppTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region URL Tab - Rules

        private void LoadRules()
        {
            var rules = UrlRuleManager.LoadRules();
            RulesDataGrid.ItemsSource = null;
            RulesDataGrid.ItemsSource = rules;
        }

        private void AddUrlDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var tag = selectedItem.Tag as string;
                if (tag == "Rule")
                {
                    AddUrlRule_Click(sender, null);
                }
                else if (tag == "Group")
                {
                    AddUrlGroup_Click(sender, null);
                }

                // Reset selection (no placeholder)
                comboBox.SelectedIndex = -1;
            }
        }

        private void AddUrlRule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddRuleWindow();
                addWindow.Owner = this;
                if (addWindow.ShowDialog() == true)
                {
                    LoadRules();
                    UpdateTabCounts();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"AddUrlRule_Click ERROR: {ex.Message}");
                MessageBox.Show($"Error opening Add Rule window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddUrlGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new EditUrlGroupWindow();
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    LoadUrlGroups();
                    UpdateTabCounts();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"AddUrlGroup_Click ERROR: {ex.Message}");
                MessageBox.Show($"Error opening URL Group window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                var rules = UrlRuleManager.LoadRules();
                var rule = rules.Find(r => r.Id == ruleId);

                if (rule != null)
                {
                    try
                    {
                        // Use the unified AddRuleWindow for editing
                        var editWindow = new AddRuleWindow(rule);
                        editWindow.Owner = this;
                        if (editWindow.ShowDialog() == true)
                        {
                            LoadRules();
                            UpdateTabCounts();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"EditRule_Click ERROR: {ex.Message}");
                        MessageBox.Show($"Error opening Edit Rule window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this rule?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UrlRuleManager.DeleteRule(ruleId);
                    LoadRules();
                    UpdateTabCounts();
                }
            }
        }

        private void ClearAllRules_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL rules? This cannot be undone.",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                UrlRuleManager.SaveRules(new List<UrlRule>());
                LoadRules();
                UpdateTabCounts();
                MessageBox.Show("All rules have been cleared.", "Rules Cleared",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MoveRuleToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                var rules = UrlRuleManager.LoadRules();
                var rule = rules.Find(r => r.Id == ruleId);

                if (rule == null) return;

                // Show choice dialog
                var result = MessageBox.Show(
                    $"Move rule '{rule.Pattern}' to a URL Group?\n\n" +
                    "• Yes = Add to URL Group\n" +
                    "• No = Keep as individual rule",
                    "Move to Group",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Move to URL Group
                    MoveRuleToUrlGroup(rule, ruleId);
                }
            }
        }

        private void MoveRuleToUrlGroup(UrlRule rule, string ruleId)
        {
            var urlGroups = UrlGroupManager.LoadGroups();
            if (urlGroups.Count == 0)
            {
                MessageBox.Show("No URL Groups exist. Create one first.", "No Groups", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Iterate through groups and ask user
            foreach (var group in urlGroups)
            {
                var addResult = MessageBox.Show(
                    $"Add pattern '{rule.Pattern}' to URL Group '{group.Name}'?",
                    "Select URL Group",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (addResult == MessageBoxResult.Cancel)
                    return;

                if (addResult == MessageBoxResult.Yes)
                {
                    if (!group.UrlPatterns.Contains(rule.Pattern))
                    {
                        group.UrlPatterns.Add(rule.Pattern);
                        UrlGroupManager.UpdateGroup(group);
                    }

                    // Ask if they want to delete the original rule
                    var deleteResult = MessageBox.Show(
                        $"Pattern added to '{group.Name}'.\n\nDelete the original individual rule?",
                        "Delete Original",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (deleteResult == MessageBoxResult.Yes)
                    {
                        UrlRuleManager.DeleteRule(ruleId);
                    }

                    LoadRules();
                    LoadUrlGroups();
                    UpdateTabCounts();
                    return;
                }
            }
        }

        #endregion

        #region URL Tab - URL Groups

        private void LoadUrlGroups()
        {
            var groups = UrlGroupManager.LoadGroups();
            UrlGroupsItemsControl.ItemsSource = null;
            UrlGroupsItemsControl.ItemsSource = groups;

            // Show/hide empty state
            UrlGroupsEmptyState.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EditUrlGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string groupId)
            {
                var group = UrlGroupManager.GetGroup(groupId);
                if (group != null)
                {
                    try
                    {
                        var editWindow = new EditUrlGroupWindow(group);
                        editWindow.Owner = this;
                        if (editWindow.ShowDialog() == true)
                        {
                            LoadUrlGroups();
                            UpdateTabCounts();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"EditUrlGroup_Click ERROR: {ex.Message}");
                        MessageBox.Show($"Error opening Edit URL Group window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteUrlGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string groupId)
            {
                var group = UrlGroupManager.GetGroup(groupId);
                if (group != null)
                {
                    string message = group.IsBuiltIn
                        ? $"Are you sure you want to delete the built-in group '{group.Name}'?\n\nIt will be re-added (disabled) when you restart the app."
                        : $"Are you sure you want to delete the URL group '{group.Name}'?\n\nThis cannot be undone.";

                    var result = MessageBox.Show(
                        message,
                        "Delete URL Group",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        UrlGroupManager.DeleteGroup(groupId);
                        LoadUrlGroups();
                        UpdateTabCounts();
                    }
                }
            }
        }

        private void UrlGroupEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.Tag is string groupId)
            {
                var group = UrlGroupManager.GetGroup(groupId);
                if (group != null)
                {
                    group.IsEnabled = toggle.IsChecked ?? false;
                    UrlGroupManager.UpdateGroup(group);
                    Logger.Log($"URL Group '{group.Name}' enabled: {group.IsEnabled}");
                }
            }
        }

        private void MovePatternToIndividualRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is IndexedUrlPattern patternInfo)
            {
                try
                {
                    var addWindow = new AddRuleWindow(patternInfo.Pattern);
                    addWindow.Owner = this;

                    if (addWindow.ShowDialog() == true)
                    {
                        // Remove pattern from URL Group (move operation)
                        var group = UrlGroupManager.GetGroup(patternInfo.GroupId);
                        if (group != null)
                        {
                            group.UrlPatterns.Remove(patternInfo.Pattern);
                            UrlGroupManager.UpdateGroup(group);
                            LoadUrlGroups();
                        }

                        LoadRules();
                        UpdateTabCounts();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"MovePatternToIndividualRule_Click ERROR: {ex.Message}");
                    MessageBox.Show($"Error creating individual rule: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region App Tab - Settings

        private void LoadAppSettings()
        {
            LoadDefaultBrowserFromRegistry();
            UpdateRegistrationStatus();
        }

        private void LoadDefaultBrowserFromRegistry()
        {
            Logger.Log("Loading default browser from registry");

            var browsers = BrowserDetector.GetInstalledBrowsers();
            DefaultBrowserComboBox.ItemsSource = browsers;

            var savedBrowser = DefaultBrowserManager.Load();
            if (savedBrowser == null)
            {
                Logger.Log("No default browser found in registry");
                return;
            }

            DefaultBrowserComboBox.SelectedItem =
                browsers.FirstOrDefault(b =>
                    b.ExecutablePath.Equals(
                        savedBrowser.ExecutablePath,
                        StringComparison.OrdinalIgnoreCase));

            Logger.Log($"Default browser loaded from registry: {savedBrowser.Name}");
        }

        private void DefaultBrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultBrowserComboBox.SelectedItem is BrowserInfo browser)
            {
                DefaultBrowserManager.Save(browser);
                Logger.Log($"User changed default browser to: {browser.Name}");
            }
        }

        private void UpdateRegistrationStatus()
        {
            bool isRegistered = IsApplicationRegistered();

            // Show/Hide buttons based on registration status
            RegisterButton.Visibility = isRegistered ? Visibility.Collapsed : Visibility.Visible;
            UnregisterButton.Visibility = isRegistered ? Visibility.Visible : Visibility.Collapsed;

            // Update the registration info text
            UpdateRegistrationInfo(isRegistered);
        }

        private bool IsApplicationRegistered()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Clients\StartMenuInternet\BrowserSelector"))
                {
                    if (key == null)
                        return false;

                    using (RegistryKey? commandKey = key.OpenSubKey(@"shell\open\command"))
                    {
                        if (commandKey != null)
                        {
                            string? registeredPath = commandKey.GetValue("")?.ToString();
                            return !string.IsNullOrEmpty(registeredPath) && registeredPath.Contains("BrowserSelector.exe");
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateRegistrationInfo(bool isRegistered)
        {
            if (isRegistered)
            {
                RegistrationStatusText.Text = "Registered";
                RegistrationStatusText.Foreground = System.Windows.Media.Brushes.Green;
                RegistrationDetailText.Text = "Browser Selector is registered and ready to use.\nSet it as default for http and https links in Windows Settings > Apps > Default apps.";
            }
            else
            {
                RegistrationStatusText.Text = "Not Registered";
                RegistrationStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                RegistrationDetailText.Text = "Click the Register button below to set up Browser Selector.";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryHelper.RegisterAsDefaultBrowser();

                MessageBox.Show(
                    "Browser Selector has been registered successfully!\n\n" +
                    "Features:\n" +
                    "- Added to Windows startup\n" +
                    "- Ready to intercept links\n\n" +
                    "To set it as default:\n" +
                    "1. Go to Windows Settings\n" +
                    "2. Apps > Default apps\n" +
                    "3. Search for 'Browser Selector'\n" +
                    "4. Set it as default for HTTP and HTTPS",
                    "Registration Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                UpdateRegistrationStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during registration: {ex.Message}",
                    "Registration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UnregisterButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to unregister Browser Selector?\n\n" +
                "This will:\n" +
                "- Remove it from Windows startup\n" +
                "- Remove it as a browser option\n" +
                "- Keep your saved rules\n\n" +
                "You can re-register anytime.",
                "Confirm Unregister",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    RegistryHelper.UnregisterAsDefaultBrowser();

                    MessageBox.Show(
                        "Browser Selector has been unregistered and removed from startup.\n\n" +
                        "Your URL rules have been preserved.",
                        "Unregistration Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    UpdateRegistrationStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during unregistration: {ex.Message}",
                        "Unregistration Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Test Automation

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var testWindow = new TestAutomationWindow();
                testWindow.Owner = this;
                testWindow.ShowDialog();

                // Reload data after tests (in case test data was created/cleaned)
                LoadAllData();
            }
            catch (Exception ex)
            {
                Logger.Log($"TestButton_Click ERROR: {ex.Message}");
                MessageBox.Show($"Error opening Test window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
