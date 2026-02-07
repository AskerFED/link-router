using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BrowserSelector.Dialogs;
using BrowserSelector.Services;

namespace BrowserSelector
{
    /// <summary>
    /// Unified window for adding and editing URL rules.
    /// Supports both single and multi-profile configurations.
    /// </summary>
    public partial class AddRuleWindow : Window
    {
        private UrlRule _existingRule;
        private bool _isEditMode;

        /// <summary>
        /// Constructor for adding a new rule
        /// </summary>
        public AddRuleWindow()
        {
            InitializeComponent();
            _isEditMode = false;
        }

        /// <summary>
        /// Constructor for adding a new rule with a suggested pattern
        /// </summary>
        public AddRuleWindow(string suggestedPattern) : this()
        {
            PatternTextBox.Text = suggestedPattern;
        }

        /// <summary>
        /// Constructor for editing an existing rule
        /// </summary>
        public AddRuleWindow(UrlRule existingRule) : this()
        {
            _existingRule = existingRule;
            _isEditMode = true;

            // Update UI for edit mode
            RuleEditorWindow.Title = "Edit URL Rule";

            // Load existing data
            PatternTextBox.Text = existingRule.Pattern;

            // Load profiles into the selector
            if (existingRule.Profiles != null && existingRule.Profiles.Count > 0)
            {
                ProfileSelector.LoadProfiles(existingRule.Profiles);
            }
            else if (!string.IsNullOrEmpty(existingRule.BrowserPath))
            {
                // Legacy single profile - load from legacy fields
                ProfileSelector.LoadSingleProfile(existingRule.BrowserPath, existingRule.ProfilePath);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous validation messages
            ValidationPanel.Clear();

            // Get profiles from the selector
            var profiles = ProfileSelector.GetAllProfiles();

            // Validate using ValidationService
            var result = ValidationService.ValidateIndividualRule(
                PatternTextBox.Text,
                profiles,
                _isEditMode ? _existingRule?.Id : null
            );

            // Show validation messages
            ValidationPanel.SetValidationResult(result);

            // Block on errors
            if (!result.IsValid)
            {
                return;
            }

            // Confirm warnings
            if (result.HasWarnings)
            {
                if (!ValidationWarningDialog.ShowWarnings(this, result.Warnings))
                {
                    return;
                }
            }

            // Use normalized pattern
            var normalizedPattern = result.NormalizedValue ?? PatternTextBox.Text.Trim();

            if (_isEditMode && _existingRule != null)
            {
                // Update existing rule
                _existingRule.Pattern = normalizedPattern;
                _existingRule.Profiles = profiles;

                // Update legacy fields from first profile for compatibility
                if (profiles.Count > 0)
                {
                    var first = profiles[0];
                    _existingRule.BrowserName = first.BrowserName;
                    _existingRule.BrowserPath = first.BrowserPath;
                    _existingRule.BrowserType = first.BrowserType;
                    _existingRule.ProfileName = first.ProfileName;
                    _existingRule.ProfilePath = first.ProfilePath;
                    _existingRule.ProfileArguments = first.ProfileArguments;
                }

                try
                {
                    UrlRuleManager.UpdateRule(_existingRule);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating rule: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Create new rule
                var rule = new UrlRule
                {
                    Pattern = normalizedPattern,
                    Profiles = profiles
                };

                // Set legacy fields from first profile for compatibility
                if (profiles.Count > 0)
                {
                    var first = profiles[0];
                    rule.BrowserName = first.BrowserName;
                    rule.BrowserPath = first.BrowserPath;
                    rule.BrowserType = first.BrowserType;
                    rule.ProfileName = first.ProfileName;
                    rule.ProfilePath = first.ProfilePath;
                    rule.ProfileArguments = first.ProfileArguments;
                }

                try
                {
                    UrlRuleManager.AddRule(rule);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving rule: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
