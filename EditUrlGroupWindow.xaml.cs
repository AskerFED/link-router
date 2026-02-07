using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrowserSelector.Dialogs;
using BrowserSelector.Models;
using BrowserSelector.Services;

namespace BrowserSelector
{
    /// <summary>
    /// Window for creating and editing URL groups.
    /// Now supports multi-profile configurations through the BrowserProfileMultiSelector control.
    /// </summary>
    public partial class EditUrlGroupWindow : Window
    {
        private UrlGroup _group;
        private bool _isEditMode;
        private bool _isBuiltIn;
        private ObservableCollection<string> _patterns;
        private List<string> _originalBuiltInPatterns = new List<string>();

        public EditUrlGroupWindow() : this(null)
        {
        }

        public EditUrlGroupWindow(UrlGroup? existingGroup)
        {
            Logger.Log("EditUrlGroupWindow: Constructor started");
            try
            {
                Logger.Log("EditUrlGroupWindow: Calling InitializeComponent...");
                InitializeComponent();
                Logger.Log("EditUrlGroupWindow: InitializeComponent completed");
            }
            catch (Exception ex)
            {
                Logger.Log($"EditUrlGroupWindow InitializeComponent ERROR: {ex.Message}");
                Logger.Log($"EditUrlGroupWindow InitializeComponent STACKTRACE: {ex.StackTrace}");
                throw;
            }

            _isEditMode = existingGroup != null;
            _group = existingGroup ?? new UrlGroup();
            _isBuiltIn = _group.IsBuiltIn;
            _patterns = new ObservableCollection<string>(_group.UrlPatterns ?? new List<string>());

            // Store original patterns for built-in groups to enable restore
            if (_isBuiltIn)
            {
                var builtInDefinition = UrlGroupManager.GetBuiltInGroups()
                    .FirstOrDefault(g => g.Id == _group.Id);
                if (builtInDefinition != null)
                {
                    _originalBuiltInPatterns = new List<string>(builtInDefinition.UrlPatterns);
                }
            }

            // Configure UI based on IsBuiltIn
            ConfigureBuiltInMode();

            if (_isEditMode)
            {
                Title = "Edit URL Group";
                GroupNameTextBox.Text = _group.Name;
                DescriptionTextBox.Text = _group.Description;

                // Load profiles into the selector
                LoadExistingProfiles();
            }
            else
            {
                Title = "New URL Group";
            }

            RefreshPatternsList();
        }

        private void ConfigureBuiltInMode()
        {
            // Show/hide Restore button based on IsBuiltIn
            RestoreButton.Visibility = _isBuiltIn ? Visibility.Visible : Visibility.Collapsed;

            // Update restore button state
            UpdateRestoreButtonState();
        }

        private void UpdateRestoreButtonState()
        {
            if (!_isBuiltIn)
            {
                RestoreButton.IsEnabled = false;
                return;
            }

            // Calculate which patterns are deleted (exist in original but not in current)
            var deletedCount = _originalBuiltInPatterns
                .Count(p => !_patterns.Contains(p));

            RestoreButton.IsEnabled = deletedCount > 0;
            RestoreButton.Content = deletedCount > 0
                ? $"Restore ({deletedCount})"
                : "Restore All";
        }

        private void LoadExistingProfiles()
        {
            // Check if group has multi-profile configuration
            if (_group.Profiles != null && _group.Profiles.Count > 0)
            {
                ProfileSelector.LoadProfiles(_group.Profiles);
            }
            else if (!string.IsNullOrEmpty(_group.DefaultBrowserPath))
            {
                // Legacy single profile - load from default fields
                ProfileSelector.LoadSingleProfile(_group.DefaultBrowserPath, _group.DefaultProfilePath);
            }
        }

        private void RefreshPatternsList()
        {
            PatternsItemsControl.ItemsSource = null;
            PatternsItemsControl.ItemsSource = _patterns.ToList();

            var count = _patterns.Count;
            PatternCountText.Text = $"{count} pattern{(count != 1 ? "s" : "")}";
            PatternCountBadge.Text = count.ToString();

            UpdateRestoreButtonState();
        }

        private void NewPatternTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddPattern();
            }
        }

        private void AddPattern_Click(object sender, RoutedEventArgs e)
        {
            AddPattern();
        }

        private void AddPattern()
        {
            string pattern = NewPatternTextBox.Text.Trim();

            if (string.IsNullOrEmpty(pattern))
            {
                return;
            }

            // Hide previous warning
            PatternWarningBorder.Visibility = Visibility.Collapsed;

            // Validate pattern format AND check for individual rule conflicts
            var result = ValidationService.ValidatePatternForGroup(pattern);

            // Show error if format invalid
            if (!result.IsValid)
            {
                var errorMessage = string.Join("\n", result.Errors.Select(e => e.Message));
                MessageBox.Show(errorMessage, "Invalid Pattern", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Use normalized pattern
            var normalizedPattern = result.NormalizedValue ?? pattern.ToLower();

            // Check for duplicate in current list
            if (_patterns.Contains(normalizedPattern))
            {
                MessageBox.Show("This pattern is already in the list.",
                    "Duplicate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show warning if pattern exists as individual rule (but still add)
            if (result.HasWarnings)
            {
                var warning = result.Warnings.First();
                PatternWarningText.Text = warning.Message;
                PatternWarningBorder.Visibility = Visibility.Visible;
                PatternWarningBorder.BringIntoView();
            }

            // Add pattern (even with warning)
            _patterns.Add(normalizedPattern);
            NewPatternTextBox.Clear();
            NewPatternTextBox.Focus();
            RefreshPatternsList();
        }

        private void RemovePattern_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pattern)
            {
                _patterns.Remove(pattern);
                RefreshPatternsList();
            }
        }

        private void RestorePatterns_Click(object sender, RoutedEventArgs e)
        {
            // Calculate which patterns are deleted
            var deletedPatterns = _originalBuiltInPatterns
                .Where(p => !_patterns.Contains(p))
                .ToList();

            if (deletedPatterns.Count == 0) return;

            foreach (var pattern in deletedPatterns)
            {
                if (!_patterns.Contains(pattern))
                {
                    _patterns.Add(pattern);
                }
            }

            RefreshPatternsList();
        }

        private void MovePatternToRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pattern)
            {
                try
                {
                    // Open AddRuleWindow with the pattern pre-filled
                    var addWindow = new AddRuleWindow(pattern);
                    addWindow.Owner = this;

                    if (addWindow.ShowDialog() == true)
                    {
                        // Remove pattern from current group
                        _patterns.Remove(pattern);
                        RefreshPatternsList();

                        // Show confirmation
                        MessageBox.Show($"Pattern '{pattern}' has been moved to an individual rule.",
                            "Pattern Moved", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"MovePatternToRule_Click ERROR: {ex.Message}");
                    MessageBox.Show($"Error moving pattern: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClosePatternWarning_Click(object sender, RoutedEventArgs e)
        {
            PatternWarningBorder.Visibility = Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous validation messages
            ValidationPanel.Clear();

            // Get profiles from the selector
            var profiles = ProfileSelector.GetAllProfiles();

            // Validate using ValidationService
            var result = ValidationService.ValidateUrlGroup(
                GroupNameTextBox.Text,
                _patterns.ToList(),
                profiles,
                _isEditMode ? _group?.Id : null
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

            // Update group
            _group.Name = GroupNameTextBox.Text.Trim();
            _group.Description = DescriptionTextBox.Text.Trim();
            _group.UrlPatterns = _patterns.ToList();
            _group.Profiles = profiles;

            // Set behavior based on profile count
            if (profiles.Count > 1)
            {
                _group.Behavior = UrlGroupBehavior.ShowProfilePicker;
            }
            else
            {
                _group.Behavior = UrlGroupBehavior.UseDefault;
            }

            // Also set legacy fields from first profile for compatibility
            if (profiles.Count > 0)
            {
                var first = profiles[0];
                _group.DefaultBrowserName = first.BrowserName;
                _group.DefaultBrowserPath = first.BrowserPath;
                _group.DefaultBrowserType = first.BrowserType;
                _group.DefaultProfileName = first.ProfileName;
                _group.DefaultProfilePath = first.ProfilePath;
                _group.DefaultProfileArguments = first.ProfileArguments;
            }

            _group.LinkedProfileGroupId = string.Empty;

            try
            {
                if (_isEditMode)
                {
                    UrlGroupManager.UpdateGroup(_group);
                }
                else
                {
                    UrlGroupManager.AddGroup(_group);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving URL group: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
