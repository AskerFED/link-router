using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
                // Extract domain for rule pattern
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

                // Open AddRuleWindow with pre-filled pattern
                var addRuleWindow = new AddRuleWindow(pattern);
                addRuleWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                if (addRuleWindow.ShowDialog() == true)
                {
                    // Rule was created - show SettingsWindow with Rules page
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.NavigateToRules();
                    settingsWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"OpenRuleCreator error: {ex.Message}");
            }
        }

        // Custom notification window
        private class CustomNotificationWindow : Window
        {
            private DispatcherTimer _autoCloseTimer;
            private readonly string _url;
            private const int NOTIFICATION_TIMEOUT = 8000; // 8 seconds
            private bool ruleWindowOpen = false;
            private Border? _progressBar;
            private double _progressBarWidth;
            private DateTime _animationStartTime;
            private double _remainingTime;

            public CustomNotificationWindow(string displayText, string url, Action MainWindowShow)
            {
                _url = url;
                _remainingTime = NOTIFICATION_TIMEOUT;

                // Window properties
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                Background = Brushes.Transparent;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                Topmost = true;
                Width = 380;
                SizeToContent = SizeToContent.Height; // Dynamic height based on content
                WindowStartupLocation = WindowStartupLocation.Manual;

                // Create UI first (needed for SizeToContent)
                Content = CreateNotificationContent(displayText, MainWindowShow);

                // Setup auto-close timer
                _autoCloseTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(NOTIFICATION_TIMEOUT)
                };
                _autoCloseTimer.Tick += (s, e) => CloseNotification();

                // Position and animate on load
                Loaded += (s, e) =>
                {
                    // Position at bottom-right after content is sized
                    var workingArea = SystemParameters.WorkArea;
                    Left = workingArea.Right - Width - 20;
                    Top = workingArea.Bottom - ActualHeight - 20;

                    AnimateIn();
                    StartProgressBarAnimation();
                    _autoCloseTimer.Start();
                    _animationStartTime = DateTime.Now;
                };

                // Pause timer and animation on mouse enter
                MouseEnter += (s, e) =>
                {
                    _autoCloseTimer.Stop();
                    _remainingTime -= (DateTime.Now - _animationStartTime).TotalMilliseconds;
                    if (_remainingTime < 0) _remainingTime = 0;
                    _progressBar?.BeginAnimation(WidthProperty, null);
                };

                // Resume timer and animation on mouse leave
                MouseLeave += (s, e) =>
                {
                    if (_remainingTime > 0)
                    {
                        _autoCloseTimer.Interval = TimeSpan.FromMilliseconds(_remainingTime);
                        _autoCloseTimer.Start();
                        _animationStartTime = DateTime.Now;
                        ResumeProgressBarAnimation();
                    }
                };
                Logger.Log("Notification constructor end");
            }

            private Border CreateNotificationContent(string displayText, Action MainWindowShow)
            {
                // Dark theme with glassmorphic effect
                var mainBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(245, 32, 32, 36)), // Dark semi-transparent
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), // Subtle white border
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(0, 0, 0),
                        Opacity = 0.4,
                        BlurRadius = 32,
                        ShadowDepth = 4,
                        Direction = 270
                    }
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // URL
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Context
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Button
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Progress bar

                // Header with icon, title and close button
                var headerGrid = new Grid { Margin = new Thickness(14, 12, 12, 4) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Close

                // App icon - using pack URI like SettingsWindow
                var appIcon = new Image
                {
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                try
                {
                    appIcon.Source = new BitmapImage(new Uri("pack://application:,,,/app.ico", UriKind.Absolute));
                }
                catch { /* Ignore icon loading errors */ }
                Grid.SetColumn(appIcon, 0);

                var titleText = new TextBlock
                {
                    Text = "LinkRouter",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(titleText, 1);

                var closeButton = CreateCloseButton();
                closeButton.Click += (s, e) => CloseNotification();
                Grid.SetColumn(closeButton, 2);

                headerGrid.Children.Add(appIcon);
                headerGrid.Children.Add(titleText);
                headerGrid.Children.Add(closeButton);
                Grid.SetRow(headerGrid, 0);

                // Status line with checkmark
                var statusPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(16, 4, 16, 2)
                };
                var checkmark = new TextBlock
                {
                    Text = "\u2713",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green for dark theme
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var statusText = new TextBlock
                {
                    Text = "Opened in default browser",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)) // Green for dark theme
                };
                statusPanel.Children.Add(checkmark);
                statusPanel.Children.Add(statusText);
                Grid.SetRow(statusPanel, 1);

                // URL display
                var urlText = new TextBlock
                {
                    Text = displayText,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)), // Light blue for dark theme
                    TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(16, 4, 16, 4)
                };
                Grid.SetRow(urlText, 2);

                // Context text
                var contextText = new TextBlock
                {
                    Text = "Create a rule to always open this domain in a specific browser.",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)), // Light gray for dark theme
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(16, 2, 16, 12)
                };
                Grid.SetRow(contextText, 3);

                // Action button
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(16, 4, 16, 14)
                };

                var createRuleButton = CreateStyledButton("Create Rule");
                createRuleButton.Click += (s, e) =>
                {
                    ruleWindowOpen = true;           // Set FIRST to prevent shutdown
                    _autoCloseTimer?.Stop();         // Stop timer to prevent race condition
                    CloseNotification();             // Close notification immediately
                    OpenRuleCreator(_url, MainWindowShow);  // Then open the rule window
                };
                buttonPanel.Children.Add(createRuleButton);
                Grid.SetRow(buttonPanel, 4);

                // Progress bar container - hidden for now
                var progressContainer = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), // Subtle light track for dark theme
                    Height = 4,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(16, 8, 16, 16),
                    ClipToBounds = true,
                    Visibility = Visibility.Collapsed // Hidden for now
                };

                _progressBarWidth = 348; // Full width minus margins (380 - 16 - 16)
                _progressBar = new Border
                {
                    Background = new LinearGradientBrush(
                        Color.FromRgb(0, 120, 212),
                        Color.FromRgb(0, 150, 255),
                        0),
                    Height = 4,
                    Width = _progressBarWidth,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(2)
                };
                progressContainer.Child = _progressBar;
                Grid.SetRow(progressContainer, 5);

                grid.Children.Add(headerGrid);
                grid.Children.Add(statusPanel);
                grid.Children.Add(urlText);
                grid.Children.Add(contextText);
                grid.Children.Add(buttonPanel);
                grid.Children.Add(progressContainer);

                mainBorder.Child = grid;
                return mainBorder;
            }

            private void AnimateIn()
            {
                var workingArea = SystemParameters.WorkArea;
                var targetLeft = workingArea.Right - Width - 20;

                // Slide in from right with CubicEase
                var slideIn = new DoubleAnimation
                {
                    From = workingArea.Right,
                    To = targetLeft,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(250)
                };

                BeginAnimation(LeftProperty, slideIn);
                BeginAnimation(OpacityProperty, fadeIn);
            }

            private void StartProgressBarAnimation()
            {
                if (_progressBar == null) return;

                var animation = new DoubleAnimation
                {
                    From = _progressBarWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(NOTIFICATION_TIMEOUT)
                };
                _progressBar.BeginAnimation(WidthProperty, animation);
            }

            private void ResumeProgressBarAnimation()
            {
                if (_progressBar == null || _remainingTime <= 0) return;

                var currentWidth = _progressBar.ActualWidth > 0 ? _progressBar.ActualWidth : (_progressBarWidth * _remainingTime / NOTIFICATION_TIMEOUT);
                var animation = new DoubleAnimation
                {
                    From = currentWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(_remainingTime)
                };
                _progressBar.BeginAnimation(WidthProperty, animation);
            }

            private Button CreateCloseButton()
            {
                var button = new Button
                {
                    Content = "✕",
                    Width = 24,
                    Height = 24,
                    FontSize = 11,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var template = new ControlTemplate(typeof(Button));

                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.Name = "border";
                borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

                borderFactory.AppendChild(contentPresenterFactory);
                template.VisualTree = borderFactory;

                // Hover - grey background
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), "border"));
                template.Triggers.Add(hoverTrigger);

                // Pressed - darker grey
                var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), "border"));
                template.Triggers.Add(pressedTrigger);

                button.Template = template;
                button.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));

                return button;
            }

            private Button CreateStyledButton(string content)
            {
                var button = new Button
                {
                    Content = content,
                    FontSize = 12,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Create ControlTemplate with rounded corners and hover effect
                var template = new ControlTemplate(typeof(Button));

                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.Name = "border";
                borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 120, 212)));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
                borderFactory.SetValue(Border.PaddingProperty, new Thickness(16, 6, 16, 6));

                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

                borderFactory.AppendChild(contentPresenterFactory);
                template.VisualTree = borderFactory;

                // Add hover trigger
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 103, 184)), "border"));
                template.Triggers.Add(hoverTrigger);

                // Add pressed trigger
                var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 90, 158)), "border"));
                template.Triggers.Add(pressedTrigger);

                button.Template = template;
                button.Foreground = Brushes.White;

                return button;
            }

            private void CloseNotification()
            {
                _autoCloseTimer?.Stop();
                _progressBar?.BeginAnimation(WidthProperty, null);

                // Slide out to right with fade
                var slideOut = new DoubleAnimation
                {
                    To = SystemParameters.WorkArea.Right,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250)
                };

                fadeOut.Completed += (s, e) => CloseAction();
                BeginAnimation(LeftProperty, slideOut);
                BeginAnimation(OpacityProperty, fadeOut);
            }
            private void CloseAction() { 
                Close();
               if(!ruleWindowOpen) Application.Current.Shutdown();
            }
        }
    }
}