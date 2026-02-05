using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BrowserSelector.Pages
{
    public partial class RulesPage : UserControl
    {
        public event EventHandler? DataChanged;

        public RulesPage()
        {
            InitializeComponent();
            Loaded += RulesPage_Loaded;
        }

        private void RulesPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        public void LoadData()
        {
            LoadRules();
            LoadUrlGroups();
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            try
            {
                var rules = UrlRuleManager.LoadRules();
                var groups = UrlGroupManager.LoadGroups();
                IndividualRulesCount.Text = $" ({rules.Count})";
                UrlGroupsCount.Text = $" ({groups.Count})";
            }
            catch { }
        }

        private void LoadRules()
        {
            try
            {
                var rules = UrlRuleManager.LoadRules();
                RulesDataGrid.ItemsSource = null;
                RulesDataGrid.ItemsSource = rules;

                // Show/hide empty state
                RulesEmptyState.Visibility = rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                RulesDataGrid.Visibility = rules.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                UpdateCounts();
            }
            catch (Exception ex)
            {
                Logger.Log($"RulesPage LoadRules ERROR: {ex.Message}");
            }
        }

        private void LoadUrlGroups()
        {
            try
            {
                var groups = UrlGroupManager.LoadGroups();
                UrlGroupsItemsControl.ItemsSource = null;
                UrlGroupsItemsControl.ItemsSource = groups;

                // Show/hide empty state
                UrlGroupsEmptyState.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                UpdateCounts();
            }
            catch (Exception ex)
            {
                Logger.Log($"RulesPage LoadUrlGroups ERROR: {ex.Message}");
            }
        }

        private void RulesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RulesDataGrid.SelectedItem is UrlRule rule)
            {
                try
                {
                    var editWindow = new AddRuleWindow(rule);
                    editWindow.Owner = Window.GetWindow(this);
                    if (editWindow.ShowDialog() == true)
                    {
                        LoadRules();
                        NotifyDataChanged();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"RulesDataGrid_MouseDoubleClick ERROR: {ex.Message}");
                }
            }
        }

        private void NotifyDataChanged()
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Add Actions

        private void AddDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var tag = selectedItem.Tag as string;
                if (tag == "Rule")
                {
                    AddUrlRule_Click();
                }
                else if (tag == "Group")
                {
                    AddUrlGroup_Click();
                }

                // Reset selection
                comboBox.SelectedIndex = -1;
            }
        }

        private void AddUrlRule_Click()
        {
            try
            {
                var addWindow = new AddRuleWindow();
                addWindow.Owner = Window.GetWindow(this);
                if (addWindow.ShowDialog() == true)
                {
                    LoadRules();
                    NotifyDataChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"AddUrlRule_Click ERROR: {ex.Message}");
                MessageBox.Show($"Error opening Add Rule window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddUrlGroup_Click()
        {
            try
            {
                var editWindow = new EditUrlGroupWindow();
                editWindow.Owner = Window.GetWindow(this);
                if (editWindow.ShowDialog() == true)
                {
                    LoadUrlGroups();
                    NotifyDataChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"AddUrlGroup_Click ERROR: {ex.Message}");
                MessageBox.Show($"Error opening URL Group window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Rule Actions

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
                        var editWindow = new AddRuleWindow(rule);
                        editWindow.Owner = Window.GetWindow(this);
                        if (editWindow.ShowDialog() == true)
                        {
                            LoadRules();
                            NotifyDataChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"EditRule_Click ERROR: {ex.Message}");
                        MessageBox.Show($"Error opening Edit Rule window: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                if (ConfirmationDialog.Show(Window.GetWindow(this), "Delete Rule",
                    "Are you sure you want to delete this rule?", "Delete", "Cancel"))
                {
                    UrlRuleManager.DeleteRule(ruleId);
                    LoadRules();
                    NotifyDataChanged();
                }
            }
        }

        private void RuleEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.Tag is string ruleId)
            {
                var rules = UrlRuleManager.LoadRules();
                var rule = rules.Find(r => r.Id == ruleId);
                if (rule != null)
                {
                    rule.IsEnabled = toggle.IsChecked ?? true;
                    UrlRuleManager.SaveRules(rules);
                    Logger.Log($"Rule '{rule.Pattern}' enabled: {rule.IsEnabled}");
                }
            }
        }

        private void ClearAllRules_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmationDialog.Show(Window.GetWindow(this), "Clear All Rules",
                "Are you sure you want to delete ALL rules? This cannot be undone.", "Delete All", "Cancel"))
            {
                UrlRuleManager.SaveRules(new List<UrlRule>());
                LoadRules();
                NotifyDataChanged();
            }
        }

        private void MoveRuleToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                var rules = UrlRuleManager.LoadRules();
                var rule = rules.Find(r => r.Id == ruleId);

                if (rule == null) return;

                if (ConfirmationDialog.Show(Window.GetWindow(this), "Move to Group",
                    $"Move rule '{rule.Pattern}' to a URL Group?", "Move", "Cancel"))
                {
                    MoveRuleToUrlGroup(rule, ruleId);
                }
            }
        }

        private void MoveRuleToUrlGroup(UrlRule rule, string ruleId)
        {
            var urlGroups = UrlGroupManager.LoadGroups();
            if (urlGroups.Count == 0)
            {
                ConfirmationDialog.Show(Window.GetWindow(this), "No Groups",
                    "No URL Groups exist. Create one first.", "OK", "Cancel");
                return;
            }

            foreach (var group in urlGroups)
            {
                if (ConfirmationDialog.Show(Window.GetWindow(this), "Select URL Group",
                    $"Add pattern '{rule.Pattern}' to URL Group '{group.Name}'?", "Add", "Skip"))
                {
                    if (!group.UrlPatterns.Contains(rule.Pattern))
                    {
                        group.UrlPatterns.Add(rule.Pattern);
                        UrlGroupManager.UpdateGroup(group);
                    }

                    if (ConfirmationDialog.Show(Window.GetWindow(this), "Delete Original",
                        $"Pattern added to '{group.Name}'.\n\nDelete the original individual rule?", "Delete", "Keep"))
                    {
                        UrlRuleManager.DeleteRule(ruleId);
                    }

                    LoadRules();
                    LoadUrlGroups();
                    NotifyDataChanged();
                    return;
                }
            }
        }

        #endregion

        #region URL Group Actions

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
                        editWindow.Owner = Window.GetWindow(this);
                        if (editWindow.ShowDialog() == true)
                        {
                            LoadUrlGroups();
                            NotifyDataChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"EditUrlGroup_Click ERROR: {ex.Message}");
                        MessageBox.Show($"Error opening Edit URL Group window: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
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

                    if (ConfirmationDialog.Show(Window.GetWindow(this), "Delete URL Group", message, "Delete", "Cancel"))
                    {
                        UrlGroupManager.DeleteGroup(groupId);
                        LoadUrlGroups();
                        NotifyDataChanged();
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
                    addWindow.Owner = Window.GetWindow(this);

                    if (addWindow.ShowDialog() == true)
                    {
                        var group = UrlGroupManager.GetGroup(patternInfo.GroupId);
                        if (group != null)
                        {
                            group.UrlPatterns.Remove(patternInfo.Pattern);
                            UrlGroupManager.UpdateGroup(group);
                            LoadUrlGroups();
                        }

                        LoadRules();
                        NotifyDataChanged();
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
    }
}
