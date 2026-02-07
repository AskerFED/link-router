using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BrowserSelector.Models;

namespace BrowserSelector.Controls
{
    /// <summary>
    /// Reusable control for displaying validation messages (errors, warnings, infos).
    /// </summary>
    public partial class ValidationMessagePanel : UserControl
    {
        private ObservableCollection<ValidationMessage> _errors = new();
        private ObservableCollection<ValidationMessage> _warnings = new();
        private ObservableCollection<ValidationMessage> _infos = new();

        public ValidationMessagePanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the panel with validation results.
        /// </summary>
        public void SetValidationResult(Models.ValidationResult? result)
        {
            if (result == null)
            {
                Clear();
                return;
            }

            _errors = new ObservableCollection<ValidationMessage>(result.Errors);
            _warnings = new ObservableCollection<ValidationMessage>(result.Warnings);
            _infos = new ObservableCollection<ValidationMessage>(result.Infos);

            ErrorsControl.ItemsSource = _errors;
            WarningsControl.ItemsSource = _warnings;
            InfosControl.ItemsSource = _infos;

            UpdateVisibility();

            // Scroll into view if there are messages
            if (result.GetAllMessages().Any())
            {
                BringIntoView();
            }
        }

        /// <summary>
        /// Sets only error messages.
        /// </summary>
        public void SetErrors(IEnumerable<ValidationMessage>? errors)
        {
            _errors = errors != null ? new ObservableCollection<ValidationMessage>(errors) : new();
            _warnings.Clear();
            _infos.Clear();

            ErrorsControl.ItemsSource = _errors;
            WarningsControl.ItemsSource = null;
            InfosControl.ItemsSource = null;

            UpdateVisibility();

            if (_errors.Any())
            {
                BringIntoView();
            }
        }

        /// <summary>
        /// Sets only warning messages.
        /// </summary>
        public void SetWarnings(IEnumerable<ValidationMessage>? warnings)
        {
            _errors.Clear();
            _warnings = warnings != null ? new ObservableCollection<ValidationMessage>(warnings) : new();
            _infos.Clear();

            ErrorsControl.ItemsSource = null;
            WarningsControl.ItemsSource = _warnings;
            InfosControl.ItemsSource = null;

            UpdateVisibility();

            if (_warnings.Any())
            {
                BringIntoView();
            }
        }

        /// <summary>
        /// Clears all validation messages.
        /// </summary>
        public void Clear()
        {
            _errors.Clear();
            _warnings.Clear();
            _infos.Clear();

            ErrorsControl.ItemsSource = null;
            WarningsControl.ItemsSource = null;
            InfosControl.ItemsSource = null;
            MessagesContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Returns true if there are any error messages.
        /// </summary>
        public bool HasErrors => _errors.Any();

        /// <summary>
        /// Returns true if there are any warning messages.
        /// </summary>
        public bool HasWarnings => _warnings.Any();

        /// <summary>
        /// Returns true if there are any messages at all.
        /// </summary>
        public bool HasMessages =>
            MessagesContainer.Visibility == Visibility.Visible;

        private void UpdateVisibility()
        {
            MessagesContainer.Visibility = (_errors.Any() || _warnings.Any() || _infos.Any())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void CloseError_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ValidationMessage message)
            {
                _errors.Remove(message);
                UpdateVisibility();
            }
        }

        private void CloseWarning_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ValidationMessage message)
            {
                _warnings.Remove(message);
                UpdateVisibility();
            }
        }

        private void CloseInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ValidationMessage message)
            {
                _infos.Remove(message);
                UpdateVisibility();
            }
        }
    }
}
