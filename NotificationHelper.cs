using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BrowserSelector
{
    public static class NotificationHelper
    {
        private static string? _lastUrl = null;
        public static void ShowNotification(string url,Action MainWindowShow)
        {
            Logger.Log($"Notification open: {url}");

            try
            {
                _lastUrl = url;

                // Extract domain for display
                string displayText;
                try
                {
                    var uri = new Uri(url);
                    displayText = uri.Host;
                }
                catch
                {
                    displayText = url.Length > 50 ? url.Substring(0, 47) + "..." : url;
                }

                // Show custom notification
                Logger.Log($"{displayText} Notification open start");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var notification = new CustomNotificationWindow(displayText, url, MainWindowShow);
                    notification.Show();
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Notification failed: {ex.Message}");
            }
        }

        private static void OpenRuleCreator(string url, Action MainWindowShow)
        {
            try
            {
                string pattern;
                try
                {
                    var uri = new Uri(url);
                    pattern = uri.Host;
                }
                catch
                {
                    pattern = url;
                }
                MainWindowShow();
            }
            catch
            {
                // Ignore errors
            }
        }

        // Custom notification window
        private class CustomNotificationWindow : Window
        {
            private DispatcherTimer _autoCloseTimer;
            private readonly string _url;
            private const int NOTIFICATION_TIMEOUT = 5000; // 5 seconds
            private bool ruleWindowOpen = false;

            public CustomNotificationWindow(string displayText, string url,Action MainWindowShow)
            {
                _url = url;

                // Window properties
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                Background = Brushes.Transparent;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                Topmost = true;
                Width = 380;
                Height = 120;
                WindowStartupLocation = WindowStartupLocation.Manual;

                // Position at bottom-right
                var workingArea = SystemParameters.WorkArea;
                Left = workingArea.Right - Width - 20;
                Top = workingArea.Bottom - Height - 20;

                // Create UI
                Content = CreateNotificationContent(displayText, MainWindowShow);

                // Setup auto-close timer
                _autoCloseTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(NOTIFICATION_TIMEOUT)
                };
                _autoCloseTimer.Tick += (s, e) => CloseNotification();
                _autoCloseTimer.Start();

                // Slide-in animation
                Loaded += (s, e) => AnimateIn();

                // Pause timer on mouse enter, resume on mouse leave
                MouseEnter += (s, e) => _autoCloseTimer.Stop();
                MouseLeave += (s, e) =>
                {
                    _autoCloseTimer.Stop();
                    _autoCloseTimer.Start();
                };
                Logger.Log("Notification constructor end");
            }

            private Border CreateNotificationContent(string displayText, Action MainWindowShow)
            {
                var mainBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.5,
                        BlurRadius = 15,
                        ShadowDepth = 3
                    }
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Header with close button
                var headerGrid = new Grid { Margin = new Thickness(15, 10, 10, 5) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = "Browser Selector",
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = Brushes.White
                };
                Grid.SetColumn(titleText, 0);

                var closeButton = new Button
                {
                    Content = "✕",
                    Width = 20,
                    Height = 20,
                    FontSize = 12,
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                closeButton.Click += (s, e) => CloseNotification();
                Grid.SetColumn(closeButton, 1);

                headerGrid.Children.Add(titleText);
                headerGrid.Children.Add(closeButton);
                Grid.SetRow(headerGrid, 0);

                // Message content
                var messageStack = new StackPanel { Margin = new Thickness(15, 5, 15, 10) };

                var messageText = new TextBlock
                {
                    Text = "URL opened in default browser:",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap
                };

                var urlText = new TextBlock
                {
                    Text = displayText,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 0)
                };

                messageStack.Children.Add(messageText);
                messageStack.Children.Add(urlText);
                Grid.SetRow(messageStack, 1);

                // Action button
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(15, 0, 15, 10)
                };

                var createRuleButton = new Button
                {
                    Content = "Create Rule",
                    Padding = new Thickness(15, 5, 15, 5),
                    Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 11
                };
                createRuleButton.Click += (s, e) =>
                {
                    OpenRuleCreator(_url, MainWindowShow);
                    ruleWindowOpen = true;
                    CloseNotification();
                };

                buttonPanel.Children.Add(createRuleButton);
                Grid.SetRow(buttonPanel, 2);

                grid.Children.Add(headerGrid);
                grid.Children.Add(messageStack);
                grid.Children.Add(buttonPanel);

                mainBorder.Child = grid;
                return mainBorder;
            }

            private void AnimateIn()
            {
                var slideIn = new DoubleAnimation
                {
                    From = SystemParameters.WorkArea.Bottom,
                    To = SystemParameters.WorkArea.Bottom - Height - 20,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                BeginAnimation(TopProperty, slideIn);
                BeginAnimation(OpacityProperty, fadeIn);
            }

            private void CloseNotification()
            {
                _autoCloseTimer?.Stop();

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                fadeOut.Completed += (s, e) => CloseAction();
                BeginAnimation(OpacityProperty, fadeOut);
            }
            private void CloseAction() { 
                Close();
               if(!ruleWindowOpen) Application.Current.Shutdown();
            }
        }
    }
}