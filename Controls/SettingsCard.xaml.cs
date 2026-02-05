using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BrowserSelector.Controls
{
    public partial class SettingsCard : UserControl
    {
        public SettingsCard()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        // Header
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        // Description
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(string.Empty, OnDescriptionChanged));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card)
            {
                card.DescriptionVisibility = string.IsNullOrEmpty(e.NewValue as string)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        // Description Visibility
        public static readonly DependencyProperty DescriptionVisibilityProperty =
            DependencyProperty.Register(nameof(DescriptionVisibility), typeof(Visibility), typeof(SettingsCard),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility DescriptionVisibility
        {
            get => (Visibility)GetValue(DescriptionVisibilityProperty);
            set => SetValue(DescriptionVisibilityProperty, value);
        }

        // Icon
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(object), typeof(SettingsCard),
                new PropertyMetadata(null, OnIconChanged));

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card)
            {
                card.IconVisibility = e.NewValue == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // Icon Visibility
        public static readonly DependencyProperty IconVisibilityProperty =
            DependencyProperty.Register(nameof(IconVisibility), typeof(Visibility), typeof(SettingsCard),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility IconVisibility
        {
            get => (Visibility)GetValue(IconVisibilityProperty);
            set => SetValue(IconVisibilityProperty, value);
        }

        // Action Content
        public static readonly DependencyProperty ActionContentProperty =
            DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(SettingsCard),
                new PropertyMetadata(null));

        public object ActionContent
        {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        // IsClickable
        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register(nameof(IsClickable), typeof(bool), typeof(SettingsCard),
                new PropertyMetadata(false, OnIsClickableChanged));

        public bool IsClickable
        {
            get => (bool)GetValue(IsClickableProperty);
            set => SetValue(IsClickableProperty, value);
        }

        private static void OnIsClickableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card)
            {
                card.CardCursor = (bool)e.NewValue ? Cursors.Hand : Cursors.Arrow;
            }
        }

        // Card Cursor
        public static readonly DependencyProperty CardCursorProperty =
            DependencyProperty.Register(nameof(CardCursor), typeof(Cursor), typeof(SettingsCard),
                new PropertyMetadata(Cursors.Arrow));

        public Cursor CardCursor
        {
            get => (Cursor)GetValue(CardCursorProperty);
            set => SetValue(CardCursorProperty, value);
        }

        // Card Background
        public static readonly DependencyProperty CardBackgroundProperty =
            DependencyProperty.Register(nameof(CardBackground), typeof(Brush), typeof(SettingsCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 255, 255))));

        public Brush CardBackground
        {
            get => (Brush)GetValue(CardBackgroundProperty);
            set => SetValue(CardBackgroundProperty, value);
        }

        // Card Border Brush
        public static readonly DependencyProperty CardBorderBrushProperty =
            DependencyProperty.Register(nameof(CardBorderBrush), typeof(Brush), typeof(SettingsCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(228, 228, 228))));

        public Brush CardBorderBrush
        {
            get => (Brush)GetValue(CardBorderBrushProperty);
            set => SetValue(CardBorderBrushProperty, value);
        }

        #endregion

        #region Click Event

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(nameof(Click), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(SettingsCard));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (IsClickable)
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (IsClickable && CardBorder != null)
            {
                CardBorder.Background = new SolidColorBrush(Color.FromRgb(242, 242, 242));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (IsClickable && CardBorder != null)
            {
                CardBorder.Background = CardBackground;
            }
        }

        #endregion
    }
}
