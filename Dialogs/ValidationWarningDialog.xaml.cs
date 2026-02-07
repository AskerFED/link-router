using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BrowserSelector.Models;

namespace BrowserSelector.Dialogs
{
    /// <summary>
    /// Dialog for confirming warnings before proceeding with save.
    /// </summary>
    public partial class ValidationWarningDialog : Window
    {
        public ValidationWarningDialog(List<ValidationMessage> warnings)
        {
            InitializeComponent();
            WarningsList.ItemsSource = warnings;
        }

        private void Proceed_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Shows the warning dialog and returns true if user chooses to proceed.
        /// Returns true if there are no warnings (nothing to confirm).
        /// </summary>
        public static bool ShowWarnings(Window owner, List<ValidationMessage>? warnings)
        {
            if (warnings == null || !warnings.Any())
                return true;

            var dialog = new ValidationWarningDialog(warnings);
            dialog.Owner = owner;
            return dialog.ShowDialog() == true;
        }

        /// <summary>
        /// Shows the warning dialog with a custom title and returns true if user chooses to proceed.
        /// </summary>
        public static bool ShowWarnings(Window owner, List<ValidationMessage>? warnings, string title)
        {
            if (warnings == null || !warnings.Any())
                return true;

            var dialog = new ValidationWarningDialog(warnings);
            dialog.Owner = owner;
            dialog.Title = title;
            return dialog.ShowDialog() == true;
        }
    }
}
