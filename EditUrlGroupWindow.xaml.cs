using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private ObservableCollection<string> _patterns;

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
            _patterns = new ObservableCollection<string>(_group.UrlPatterns ?? new List<string>());

            if (_isEditMode)
            {
                HeaderText.Text = "Edit URL Group";
                Title = "Edit URL Group";
                GroupNameTextBox.Text = _group.Name;
                DescriptionTextBox.Text = _group.Description;

                // Load profiles into the selector
                LoadExistingProfiles();
            }
            else
            {
                HeaderText.Text = "New URL Group";
                Title = "New URL Group";
            }

            RefreshPatternsList();
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
            PatternsItemsControl.ItemsSource = _patterns;
            PatternCountText.Text = $"{_patterns.Count} pattern{(_patterns.Count != 1 ? "s" : "")}";
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
            string pattern = NewPatternTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(pattern))
            {
                return;
            }

            // Remove protocol if included
            if (pattern.StartsWith("http://"))
                pattern = pattern.Substring(7);
            if (pattern.StartsWith("https://"))
                pattern = pattern.Substring(8);

            // Remove trailing slash
            pattern = pattern.TrimEnd('/');

            if (_patterns.Contains(pattern))
            {
                MessageBox.Show("This pattern is already in the list.",
                    "Duplicate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _patterns.Add(pattern);
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate name
            string name = GroupNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a group name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupNameTextBox.Focus();
                return;
            }

            // Validate patterns
            if (_patterns.Count == 0)
            {
                MessageBox.Show("Please add at least one URL pattern.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPatternTextBox.Focus();
                return;
            }

            // Validate browser/profile selection
            if (!ProfileSelector.HasValidSelection())
            {
                MessageBox.Show("Please select a browser and profile.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update group
            _group.Name = name;
            _group.Description = DescriptionTextBox.Text.Trim();
            _group.UrlPatterns = _patterns.ToList();

            // Get profiles from the selector
            var profiles = ProfileSelector.GetAllProfiles();
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
