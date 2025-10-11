using System;
using System.Windows;
using System.Windows.Controls;

namespace BrowserSelector
{
    public partial class RulesManagerWindow : Window
    {
        public RulesManagerWindow()
        {
            InitializeComponent();
            LoadRules();
        }

        private void LoadRules()
        {
            var rules = UrlRuleManager.LoadRules();
            RulesDataGrid.ItemsSource = null;
            RulesDataGrid.ItemsSource = rules;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ruleId)
            {
                var rules = UrlRuleManager.LoadRules();
                var rule = rules.Find(r => r.Id == ruleId);

                if (rule != null)
                {
                    var editWindow = new EditRuleWindow(rule);
                    editWindow.Owner = this;
                    if (editWindow.ShowDialog() == true)
                    {
                        LoadRules();
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
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
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL rules? This cannot be undone.",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                UrlRuleManager.SaveRules(new System.Collections.Generic.List<UrlRule>());
                LoadRules();
                MessageBox.Show("All rules have been cleared.", "Rules Cleared",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}