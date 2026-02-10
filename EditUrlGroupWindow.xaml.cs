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
        private ObservableCollection<UrlPattern> _patterns;
        private List<UrlPattern> _filteredPatterns = new List<UrlPattern>();
        private List<UrlPattern> _originalBuiltInPatterns = new List<UrlPattern>();

        // Change detection fields for edit mode
        private string _initialName = "";
        private string _initialDescription = "";
        private bool _initialClipboardNotify = true;
        private List<string> _initialPatternStrings = new List<string>();
        private List<RuleProfile> _initialProfiles = new List<RuleProfile>();

        /// <summary>
        /// Indicates if individual rules were added via "Move to Rule" during this session.
        /// The caller should check this to refresh the rules list.
        /// </summary>
        public bool RulesWereModified { get; private set; }

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
            _patterns = new ObservableCollection<UrlPattern>(_group.UrlPatterns ?? new List<UrlPattern>());

            // Store original patterns for built-in groups to enable restore
            if (_isBuiltIn)
            {
                // Load original patterns from the template manifest
                var manifest = Services.BuiltInTemplateManager.LoadTemplateManifest();
                var builtInTemplate = manifest.Templates.FirstOrDefault(t => t.Id == _group.Id);
                if (builtInTemplate != null)
                {
                    _originalBuiltInPatterns = builtInTemplate.Patterns.Select(p => new UrlPattern
                    {
                        Id = p.Id,
                        Pattern = p.Pattern,
                        IsBuiltIn = true,
                        VersionAdded = p.VersionAdded
                    }).ToList();
                }
            }

            // Configure UI based on IsBuiltIn
            ConfigureBuiltInMode();

            if (_isEditMode)
            {
                Title = "Edit URL Group";
                GroupNameTextBox.Text = _group.Name;
                DescriptionTextBox.Text = _group.Description;

                // Load clipboard notification setting
                ClipboardNotifyToggle.IsChecked = _group.ClipboardNotificationsEnabled;

                // Load profiles into the selector
                LoadExistingProfiles();

                // Store initial values for change detection
                _initialName = _group.Name ?? "";
                _initialDescription = _group.Description ?? "";
                _initialClipboardNotify = _group.ClipboardNotificationsEnabled;
                _initialPatternStrings = _patterns.Select(p => p.Pattern).ToList();
                _initialProfiles = _group.Profiles?.Select(p => new RuleProfile
                {
                    BrowserName = p.BrowserName,
                    BrowserPath = p.BrowserPath,
                    BrowserType = p.BrowserType,
                    ProfileName = p.ProfileName,
                    ProfilePath = p.ProfilePath,
                    ProfileArguments = p.ProfileArguments
                }).ToList() ?? new List<RuleProfile>();

                // Hide Save button until changes detected
                SaveButton.Visibility = Visibility.Collapsed;

                // Wire up change detection events
                GroupNameTextBox.TextChanged += (s, e) => CheckForChanges();
                DescriptionTextBox.TextChanged += (s, e) => CheckForChanges();
                ClipboardNotifyToggle.Checked += (s, e) => CheckForChanges();
                ClipboardNotifyToggle.Unchecked += (s, e) => CheckForChanges();
                ProfileSelector.SelectionChanged += (s, e) => CheckForChanges();
            }
            else
            {
                Title = "New URL Group";
                SaveButton.Content = "Add";
            }

            // Disable clipboard toggle if global clipboard monitoring is off
            var globalSettings = SettingsManager.LoadSettings();
            if (!globalSettings.ClipboardMonitoring.IsEnabled)
            {
                ClipboardNotifyToggle.IsEnabled = false;
                ClipboardNotifyPanel.Opacity = 0.5;
                ClipboardNotifyPanel.ToolTip = "Enable clipboard monitoring in Settings to use this feature";
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
            var currentPatternStrings = _patterns.Select(p => p.Pattern.ToLower()).ToHashSet();
            var deletedCount = _originalBuiltInPatterns
                .Count(p => !currentPatternStrings.Contains(p.Pattern.ToLower()));

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
            string searchText = NewPatternTextBox?.Text?.Trim().ToLower() ?? "";

            // Use filtered list for display when searching
            var displayList = string.IsNullOrEmpty(searchText)
                ? _patterns.ToList()
                : _filteredPatterns;

            PatternsItemsControl.ItemsSource = null;
            PatternsItemsControl.ItemsSource = displayList;

            // Show empty state when: no patterns at all, OR search with no results
            bool hasSearchText = !string.IsNullOrEmpty(searchText);
            bool showEmptyState = _patterns.Count == 0 || (hasSearchText && displayList.Count == 0);

            NoResultsPanel.Visibility = showEmptyState ? Visibility.Visible : Visibility.Collapsed;
            PatternsListBorder.Visibility = showEmptyState ? Visibility.Collapsed : Visibility.Visible;

            // Update empty state text based on context
            if (_patterns.Count == 0)
            {
                NoResultsText.Text = "No patterns yet";
                NoResultsHint.Text = "Add a pattern above to get started";
            }
            else
            {
                NoResultsText.Text = "No URL found";
                NoResultsHint.Text = "Press Enter or click Add to create this pattern";
            }

            // Update count text
            var totalCount = _patterns.Count;
            if (hasSearchText && displayList.Count != totalCount)
            {
                PatternCountText.Text = $"{displayList.Count} of {totalCount} patterns";
            }
            else
            {
                PatternCountText.Text = $"{totalCount} pattern{(totalCount != 1 ? "s" : "")}";
            }
            PatternCountBadge.Text = totalCount.ToString();

            UpdateRestoreButtonState();
        }

        private void CheckForChanges()
        {
            if (!_isEditMode) return;

            var currentPatternStrings = _patterns.Select(p => p.Pattern).ToList();
            bool patternsChanged = !PatternsAreEqual(_initialPatternStrings, currentPatternStrings);

            var currentProfiles = ProfileSelector.GetAllProfiles();
            bool profilesChanged = !ProfilesAreEqual(_initialProfiles, currentProfiles);

            bool hasChanges = GroupNameTextBox.Text.Trim() != _initialName
                || DescriptionTextBox.Text.Trim() != _initialDescription
                || ClipboardNotifyToggle.IsChecked != _initialClipboardNotify
                || patternsChanged
                || profilesChanged;

            SaveButton.Visibility = hasChanges ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool PatternsAreEqual(List<string> initial, List<string> current)
        {
            if (initial.Count != current.Count) return false;
            var sortedInitial = initial.OrderBy(p => p).ToList();
            var sortedCurrent = current.OrderBy(p => p).ToList();
            return sortedInitial.SequenceEqual(sortedCurrent);
        }

        private static bool ProfilesAreEqual(List<RuleProfile> initial, List<RuleProfile> current)
        {
            if (initial.Count != current.Count) return false;
            for (int i = 0; i < initial.Count; i++)
            {
                if (initial[i].BrowserPath != current[i].BrowserPath ||
                    initial[i].ProfilePath != current[i].ProfilePath)
                    return false;
            }
            return true;
        }

        private void NewPatternTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterPatterns();
        }

        private void FilterPatterns()
        {
            string searchText = NewPatternTextBox?.Text?.Trim().ToLower() ?? "";

            // Update placeholder visibility
            if (PatternPlaceholder != null)
            {
                PatternPlaceholder.Visibility = string.IsNullOrEmpty(NewPatternTextBox?.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
            }

            // Update clear button visibility
            if (ClearPatternButton != null)
            {
                ClearPatternButton.Visibility = string.IsNullOrEmpty(NewPatternTextBox?.Text)
                    ? Visibility.Collapsed : Visibility.Visible;
            }

            if (string.IsNullOrEmpty(searchText))
            {
                // Show all patterns
                _filteredPatterns = _patterns.ToList();
                if (AddPatternButton != null)
                    AddPatternButton.IsEnabled = false;
            }
            else
            {
                // Filter patterns that contain the search text
                _filteredPatterns = _patterns
                    .Where(p => p.Pattern.ToLower().Contains(searchText))
                    .ToList();

                // Enable Add button only when no matches found and pattern doesn't already exist
                bool exactMatch = _patterns.Any(p => p.Pattern.ToLower() == searchText);
                if (AddPatternButton != null)
                    AddPatternButton.IsEnabled = _filteredPatterns.Count == 0 && !exactMatch;
            }

            RefreshPatternsList();
        }

        private void ClearPattern_Click(object sender, RoutedEventArgs e)
        {
            NewPatternTextBox.Clear();
            NewPatternTextBox.Focus();
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

            // Validate pattern format AND check for individual rule conflicts AND other group conflicts
            var result = ValidationService.ValidatePatternForGroup(pattern, _isEditMode ? _group?.Id : null);

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
            if (_patterns.Any(p => p.Pattern.ToLower() == normalizedPattern.ToLower()))
            {
                MessageBox.Show("This pattern is already in the list.",
                    "Duplicate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Block adding if pattern exists in individual rules or other groups
            if (result.HasWarnings)
            {
                // Combine all warning messages and show as error (block adding)
                var errorMessages = string.Join("\n", result.Warnings.Select(w => w.Message));
                PatternWarningText.Text = errorMessages;
                PatternWarningBorder.Visibility = Visibility.Visible;
                PatternWarningBorder.BringIntoView();
                // Don't clear search text - let user see what they tried to add
                return;
            }

            // Add pattern as new UrlPattern object
            _patterns.Add(new UrlPattern
            {
                Pattern = normalizedPattern,
                IsBuiltIn = false,
                CreatedDate = DateTime.Now
            });
            NewPatternTextBox.Clear();
            NewPatternTextBox.Focus();
            FilterPatterns(); // Reset filter state after adding
            CheckForChanges(); // Update save button visibility

            // Scroll to bottom to show newly added pattern (after layout update)
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                PatternsScrollViewer.ScrollToEnd();
            }));
        }

        private void RemovePattern_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                UrlPattern? patternToRemove = null;
                string patternText = "";

                // Handle both UrlPattern object binding and string binding
                if (button.Tag is UrlPattern urlPattern)
                {
                    patternToRemove = urlPattern;
                    patternText = urlPattern.Pattern;
                }
                else if (button.DataContext is UrlPattern dcPattern)
                {
                    patternToRemove = dcPattern;
                    patternText = dcPattern.Pattern;
                }

                if (patternToRemove != null)
                {
                    if (ConfirmationDialog.Show(this, "Remove Pattern",
                        $"Are you sure you want to remove '{patternText}' from this group?", "Remove", "Cancel"))
                    {
                        // Track deleted built-in patterns so they won't be re-added on template updates
                        if (patternToRemove.IsBuiltIn && !string.IsNullOrEmpty(patternToRemove.Id))
                        {
                            if (!_group.DeletedBuiltInPatternIds.Contains(patternToRemove.Id))
                            {
                                _group.DeletedBuiltInPatternIds.Add(patternToRemove.Id);
                            }
                        }

                        _patterns.Remove(patternToRemove);
                        RefreshPatternsList();
                        CheckForChanges(); // Update save button visibility
                    }
                }
            }
        }

        private void RestorePatterns_Click(object sender, RoutedEventArgs e)
        {
            // Calculate which patterns are deleted
            var currentPatternStrings = _patterns.Select(p => p.Pattern.ToLower()).ToHashSet();
            var deletedPatterns = _originalBuiltInPatterns
                .Where(p => !currentPatternStrings.Contains(p.Pattern.ToLower()))
                .ToList();

            if (deletedPatterns.Count == 0) return;

            foreach (var pattern in deletedPatterns)
            {
                if (!currentPatternStrings.Contains(pattern.Pattern.ToLower()))
                {
                    // Add restored pattern with updated timestamp
                    _patterns.Add(new UrlPattern
                    {
                        Id = pattern.Id,
                        Pattern = pattern.Pattern,
                        IsBuiltIn = true,
                        VersionAdded = pattern.VersionAdded,
                        CreatedDate = DateTime.Now
                    });

                    // Remove from deleted list if it was tracked
                    _group.DeletedBuiltInPatternIds.Remove(pattern.Id);
                }
            }

            RefreshPatternsList();
            CheckForChanges(); // Update save button visibility
        }

        private void MovePatternToRule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                UrlPattern? patternToMove = null;

                // Handle both UrlPattern object binding and DataContext
                if (button.Tag is UrlPattern urlPattern)
                {
                    patternToMove = urlPattern;
                }
                else if (button.DataContext is UrlPattern dcPattern)
                {
                    patternToMove = dcPattern;
                }

                if (patternToMove != null)
                {
                    try
                    {
                        // Open AddRuleWindow with the pattern pre-filled, passing group ID to exclude from validation
                        var addWindow = new AddRuleWindow(patternToMove.Pattern, _group.Id);
                        addWindow.Owner = this;

                        if (addWindow.ShowDialog() == true)
                        {
                            // Track deleted built-in patterns
                            if (patternToMove.IsBuiltIn && !string.IsNullOrEmpty(patternToMove.Id))
                            {
                                if (!_group.DeletedBuiltInPatternIds.Contains(patternToMove.Id))
                                {
                                    _group.DeletedBuiltInPatternIds.Add(patternToMove.Id);
                                }
                            }

                            // Remove pattern from current group
                            _patterns.Remove(patternToMove);
                            RefreshPatternsList();
                            CheckForChanges(); // Update save button visibility

                            // Flag that rules were modified so caller can refresh rules list
                            RulesWereModified = true;

                            // Show confirmation
                            MessageBox.Show($"Pattern '{patternToMove.Pattern}' has been moved to an individual rule.",
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

            // Validate using ValidationService (pass pattern strings for validation)
            var result = ValidationService.ValidateUrlGroup(
                GroupNameTextBox.Text,
                _patterns.Select(p => p.Pattern).ToList(),
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
            _group.ModifiedDate = DateTime.Now;
            _group.Profiles = profiles;
            _group.ClipboardNotificationsEnabled = ClipboardNotifyToggle.IsChecked == true;

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
