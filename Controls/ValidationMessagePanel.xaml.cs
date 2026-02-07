using System.Collections.Generic;
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

            ErrorsControl.ItemsSource = result.Errors;
            WarningsControl.ItemsSource = result.Warnings;
            InfosControl.ItemsSource = result.Infos;

            MessagesContainer.Visibility = result.GetAllMessages().Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Sets only error messages.
        /// </summary>
        public void SetErrors(IEnumerable<ValidationMessage>? errors)
        {
            ErrorsControl.ItemsSource = errors;
            WarningsControl.ItemsSource = null;
            InfosControl.ItemsSource = null;

            MessagesContainer.Visibility = errors?.Any() == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Sets only warning messages.
        /// </summary>
        public void SetWarnings(IEnumerable<ValidationMessage>? warnings)
        {
            ErrorsControl.ItemsSource = null;
            WarningsControl.ItemsSource = warnings;
            InfosControl.ItemsSource = null;

            MessagesContainer.Visibility = warnings?.Any() == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Clears all validation messages.
        /// </summary>
        public void Clear()
        {
            ErrorsControl.ItemsSource = null;
            WarningsControl.ItemsSource = null;
            InfosControl.ItemsSource = null;
            MessagesContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Returns true if there are any error messages.
        /// </summary>
        public bool HasErrors =>
            ErrorsControl.ItemsSource is IEnumerable<ValidationMessage> errors && errors.Any();

        /// <summary>
        /// Returns true if there are any warning messages.
        /// </summary>
        public bool HasWarnings =>
            WarningsControl.ItemsSource is IEnumerable<ValidationMessage> warnings && warnings.Any();

        /// <summary>
        /// Returns true if there are any messages at all.
        /// </summary>
        public bool HasMessages =>
            MessagesContainer.Visibility == Visibility.Visible;
    }
}
