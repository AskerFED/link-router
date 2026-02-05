using System.Windows;
using System.Windows.Input;

namespace BrowserSelector
{
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog(string title, string message, string yesText = "Yes", string noText = "No")
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            YesButton.Content = yesText;
            NoButton.Content = noText;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            // Clicking backdrop cancels the dialog
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Shows a confirmation dialog and returns true if user clicks Yes
        /// </summary>
        public static bool Show(Window owner, string title, string message, string yesText = "Yes", string noText = "No")
        {
            var dialog = new ConfirmationDialog(title, message, yesText, noText);
            dialog.Owner = owner;
            return dialog.ShowDialog() == true;
        }

        /// <summary>
        /// Shows a confirmation dialog with default Yes/No buttons
        /// </summary>
        public static bool Confirm(Window owner, string message, string title = "Confirm")
        {
            return Show(owner, title, message);
        }

        /// <summary>
        /// Shows a warning confirmation dialog
        /// </summary>
        public static bool ConfirmWarning(Window owner, string message, string title = "Warning")
        {
            return Show(owner, title, message, "Delete", "Cancel");
        }
    }
}
