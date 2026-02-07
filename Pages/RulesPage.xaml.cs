using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserSelector.Pages
{
    public partial class RulesPage : UserControl
    {
        public event EventHandler? DataChanged;
        private UrlRule? _ruleBeingMoved = null;
        private List<UrlRule> _allRules = new List<UrlRule>();
        private List<UrlGroup> _allGroups = new List<UrlGroup>();

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
                var rulesCount = _allRules?.Count ?? 0;
                var groupsCount = _allGroups?.Count ?? 0;

                RulesCountBadge.Text = rulesCount.ToString();
                GroupsCountBadge.Text = groupsCount.ToString();
            }
            catch { }
        }

        private void LoadRules()
        {
            try
            {
                _allRules = UrlRuleManager.LoadRules();
                RulesDataGrid.ItemsSource = null;
                RulesDataGrid.ItemsSource = _allRules;

                // Clear search box
                if (RulesSearchBox != null)
                    RulesSearchBox.Text = "";

                // Show/hide empty state
                RulesEmptyState.Visibility = _allRules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                RulesDataGrid.Visibility = _allRules.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

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
                _allGroups = UrlGroupManager.LoadGroups();
                UrlGroupsItemsControl.ItemsSource = null;
                UrlGroupsItemsControl.ItemsSource = _allGroups;

                // Clear search box
                if (GroupsSearchBox != null)
                    GroupsSearchBox.Text = "";

                // Show/hide empty state
                UrlGroupsEmptyState.Visibility = _allGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

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

        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            AddUrlRule_Click();
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            AddUrlGroup_Click();
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

                    // Refresh DataGrid to update row opacity
                    RulesDataGrid.Items.Refresh();
                }
            }
        }

        private void ClearIndividualRules_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmationDialog.Show(Window.GetWindow(this), "Clear Individual Rules",
                "Are you sure you want to delete all individual rules? This cannot be undone.", "Delete All", "Cancel"))
            {
                UrlRuleManager.SaveRules(new List<UrlRule>());
                LoadRules();
                NotifyDataChanged();
            }
        }

        private void ClearUrlGroups_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmationDialog.Show(Window.GetWindow(this), "Clear URL Groups",
                "Are you sure you want to clear all URL groups? Built-in groups will be disabled, custom groups will be deleted.", "Clear All", "Cancel"))
            {
                var groups = UrlGroupManager.LoadGroups();

                // Keep built-in groups but disable them
                var builtInGroups = groups.Where(g => g.IsBuiltIn).ToList();
                foreach (var group in builtInGroups)
                {
                    group.IsEnabled = false;
                }

                UrlGroupManager.SaveGroups(builtInGroups);
                LoadUrlGroups();
                NotifyDataChanged();
            }
        }

        private void MoveRuleToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                var rule = UrlRuleManager.LoadRules().FirstOrDefault(r => r.Id == ruleId);
                if (rule == null) return;

                var groups = UrlGroupManager.LoadGroups().Where(g => g.IsEnabled).ToList();

                if (groups.Count == 0)
                {
                    MessageBox.Show("No URL Groups exist. Create a group first in the 'URL Groups' tab.",
                        "No Groups", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _ruleBeingMoved = rule;
                GroupSelectionList.ItemsSource = groups;

                // Position popup near the button
                MoveToGroupPopup.PlacementTarget = button;
                MoveToGroupPopup.IsOpen = true;
            }
        }

        private void GroupSelectionItem_Click(object sender, RoutedEventArgs e)
        {
            MoveToGroupPopup.IsOpen = false;

            if (sender is Button button && button.Tag is string groupId && _ruleBeingMoved != null)
            {
                var group = UrlGroupManager.GetGroup(groupId);
                if (group == null) return;

                // Add pattern to group
                if (!group.UrlPatterns.Contains(_ruleBeingMoved.Pattern))
                {
                    group.UrlPatterns.Add(_ruleBeingMoved.Pattern);
                    UrlGroupManager.UpdateGroup(group);
                }

                // Ask to delete original rule
                var result = MessageBox.Show(
                    $"Pattern added to '{group.Name}'.\n\nDelete the original individual rule?",
                    "Rule Moved",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UrlRuleManager.DeleteRule(_ruleBeingMoved.Id);
                }

                _ruleBeingMoved = null;
                LoadData();
                NotifyDataChanged();
            }
        }

        #endregion

        #region URL Group Actions

        private void UrlGroupCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Don't open edit if clicking on buttons or toggle
            if (e.OriginalSource is System.Windows.Controls.Primitives.ToggleButton ||
                e.OriginalSource is Button ||
                (e.OriginalSource as FrameworkElement)?.TemplatedParent is Button ||
                (e.OriginalSource as FrameworkElement)?.TemplatedParent is System.Windows.Controls.Primitives.ToggleButton)
            {
                return;
            }

            if (sender is Border border && border.Tag is string groupId)
            {
                OpenEditUrlGroup(groupId);
            }
        }

        private void EditUrlGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string groupId)
            {
                OpenEditUrlGroup(groupId);
            }
        }

        private void OpenEditUrlGroup(string groupId)
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
                    Logger.Log($"OpenEditUrlGroup ERROR: {ex.Message}");
                    MessageBox.Show($"Error opening Edit URL Group window: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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

                    // Refresh to update visual state
                    LoadUrlGroups();
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

        #region Scroll Handling

        /// <summary>
        /// Bubbles scroll events from DataGrid to parent ScrollViewer
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is DataGrid dataGrid && !e.Handled)
            {
                // Bubble the event to the parent ScrollViewer
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                var parent = dataGrid.Parent as UIElement;
                while (parent != null)
                {
                    if (parent is ScrollViewer)
                    {
                        parent.RaiseEvent(eventArg);
                        break;
                    }
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent) as UIElement;
                }
            }
        }

        #endregion

        #region Search Filtering

        private void RulesSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = RulesSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            if (string.IsNullOrEmpty(filter))
            {
                RulesDataGrid.ItemsSource = _allRules;
            }
            else
            {
                RulesDataGrid.ItemsSource = _allRules
                    .Where(r => r.Pattern.ToLowerInvariant().Contains(filter) ||
                               (r.BrowserName?.ToLowerInvariant().Contains(filter) ?? false) ||
                               (r.ProfileName?.ToLowerInvariant().Contains(filter) ?? false))
                    .ToList();
            }
        }

        private void GroupsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = GroupsSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            if (string.IsNullOrEmpty(filter))
            {
                UrlGroupsItemsControl.ItemsSource = _allGroups;
            }
            else
            {
                UrlGroupsItemsControl.ItemsSource = _allGroups
                    .Where(g => g.Name.ToLowerInvariant().Contains(filter) ||
                               (g.Description?.ToLowerInvariant().Contains(filter) ?? false) ||
                                g.UrlPatterns.Any(p => p.ToLowerInvariant().Contains(filter)))
                    .ToList();
            }
        }

        private void ClearRulesSearch_Click(object sender, RoutedEventArgs e)
        {
            RulesSearchBox.Text = "";
            RulesSearchBox.Focus();
        }

        private void ClearGroupsSearch_Click(object sender, RoutedEventArgs e)
        {
            GroupsSearchBox.Text = "";
            GroupsSearchBox.Focus();
        }

        #endregion

        #region Pattern Testing

        private void TestUrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TestUrl_Click(sender, e);
            }
        }

        private void TestUrl_Click(object sender, RoutedEventArgs e)
        {
            var url = TestUrlTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(url))
            {
                ShowTestResult(null, "Enter a URL to test");
                return;
            }

            try
            {
                // Check URL Groups first (higher priority)
                var (matchedGroup, _) = UrlGroupManager.FindMatchingGroup(url);
                if (matchedGroup != null && matchedGroup.IsEnabled)
                {
                    ShowTestResult(true, $"Matches group: {matchedGroup.Name}");
                    return;
                }

                // Check Individual Rules
                var matchedRule = UrlRuleManager.FindMatchingRule(url);
                if (matchedRule != null && matchedRule.IsEnabled)
                {
                    ShowTestResult(true, $"Matches rule: {matchedRule.Pattern}");
                    return;
                }

                ShowTestResult(false, "No matching rule or group");
            }
            catch (Exception ex)
            {
                Logger.Log($"TestUrl_Click ERROR: {ex.Message}");
                ShowTestResult(false, "Error testing URL");
            }
        }

        private void ShowTestResult(bool? isMatch, string message)
        {
            TestResultPanel.Visibility = Visibility.Visible;

            if (isMatch == null)
            {
                // Neutral state (e.g., empty input)
                TestResultIcon.Text = "\uE946"; // Info icon
                TestResultIcon.Foreground = (Brush)FindResource("TextSecondaryBrush");
                TestResultText.Foreground = (Brush)FindResource("TextSecondaryBrush");
            }
            else if (isMatch == true)
            {
                TestResultIcon.Text = "\uE73E"; // Checkmark
                TestResultIcon.Foreground = (Brush)FindResource("SuccessBrush");
                TestResultText.Foreground = (Brush)FindResource("SuccessBrush");
            }
            else
            {
                TestResultIcon.Text = "\uE711"; // X mark
                TestResultIcon.Foreground = (Brush)FindResource("DangerBrush");
                TestResultText.Foreground = (Brush)FindResource("DangerBrush");
            }

            TestResultText.Text = message;
        }

        #endregion
    }
}
