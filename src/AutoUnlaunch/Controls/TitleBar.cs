using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace MrCapitalQ.AutoUnlaunch.Controls;

public sealed class TitleBar : Control
{
    public static readonly DependencyProperty IsBackButtonVisibleProperty =
       DependencyProperty.Register(nameof(IsBackButtonVisible),
          typeof(bool),
          typeof(TitleBar),
          new PropertyMetadata(false));
    public static readonly DependencyProperty IconProperty =
       DependencyProperty.Register(nameof(Icon),
          typeof(ImageSource),
          typeof(TitleBar),
          new PropertyMetadata(default(ImageSource)));
    public static readonly DependencyProperty TitleProperty =
       DependencyProperty.Register(nameof(Title),
          typeof(string),
          typeof(TitleBar),
          new PropertyMetadata(default(string)));
    public static readonly DependencyProperty WindowProperty =
       DependencyProperty.Register(nameof(Window),
          typeof(Window),
          typeof(TitleBar),
          new PropertyMetadata(default(Window)));
    public static readonly DependencyProperty PreferredHeightProperty =
       DependencyProperty.Register(nameof(PreferredHeight),
          typeof(TitleBarHeightOption),
          typeof(TitleBar),
          new PropertyMetadata(default(TitleBarHeightOption)));
    public static readonly DependencyProperty LeftInsetProperty =
       DependencyProperty.Register(nameof(LeftInset),
          typeof(double),
          typeof(TitleBar),
          new PropertyMetadata(default(double)));
    public static readonly DependencyProperty RightInsetProperty =
       DependencyProperty.Register(nameof(RightInset),
          typeof(double),
          typeof(TitleBar),
          new PropertyMetadata(default(double)));
    public static readonly DependencyProperty BackButtonVisibilityProperty =
       DependencyProperty.Register(nameof(BackButtonVisibility),
          typeof(Visibility),
          typeof(TitleBar),
          new PropertyMetadata(Visibility.Collapsed));
    public static readonly DependencyProperty IconVisibilityProperty =
       DependencyProperty.Register(nameof(IconVisibility),
          typeof(Visibility),
          typeof(TitleBar),
          new PropertyMetadata(Visibility.Collapsed));

    public event EventHandler? BackRequested;

    public TitleBar()
    {
        DefaultStyleKey = typeof(TitleBar);
        Loaded += TitleBar_Loaded;
    }

    public bool IsBackButtonVisible
    {
        get => GetValue(IsBackButtonVisibleProperty) as bool? ?? false;
        set
        {
            SetValue(IsBackButtonVisibleProperty, value);
            BackButtonVisibility = value is true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public ImageSource? Icon
    {
        get => GetValue(IconProperty) as ImageSource;
        set
        {
            SetValue(IconProperty, value);
            IconVisibility = value is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public string? Title
    {
        get => GetValue(TitleProperty)?.ToString();
        set => SetValue(TitleProperty, value);
    }

    public Window? Window
    {
        get => GetValue(WindowProperty) as Window;
        set
        {
            SetValue(WindowProperty, value);
            UpdateTitleBarLayout();
            if (value is not null)
                value.Activated += Window_Activated;
        }
    }

    public TitleBarHeightOption PreferredHeight
    {
        get => (TitleBarHeightOption)GetValue(PreferredHeightProperty);
        set
        {
            SetValue(PreferredHeightProperty, value);
            UpdateTitleBarLayout();
        }
    }

    public double LeftInset
    {
        get => GetValue(LeftInsetProperty) as double? ?? 0;
        private set => SetValue(LeftInsetProperty, Math.Max(0, value));
    }

    public double RightInset
    {
        get => GetValue(RightInsetProperty) as double? ?? 0;
        private set => SetValue(RightInsetProperty, Math.Max(0, value));
    }

    public Visibility BackButtonVisibility
    {
        get => GetValue(BackButtonVisibilityProperty) as Visibility? ?? Visibility.Collapsed;
        private set => SetValue(BackButtonVisibilityProperty, value);
    }

    public Visibility IconVisibility
    {
        get => GetValue(IconVisibilityProperty) as Visibility? ?? Visibility.Collapsed;
        private set => SetValue(IconVisibilityProperty, value);
    }

    private void UpdateTitleBarLayout()
    {
        if (Window?.AppWindow?.TitleBar is null)
            return;

        Window.AppWindow.TitleBar.PreferredHeightOption = PreferredHeight;

        var rasterizationScale = XamlRoot?.RasterizationScale ?? 1;
        Height = Window.AppWindow.TitleBar.Height / rasterizationScale;
        LeftInset = Window.AppWindow.TitleBar.LeftInset / rasterizationScale;
        RightInset = Window.AppWindow.TitleBar.RightInset / rasterizationScale;
    }

    private void UpdateClickThroughRegions()
    {
        if (GetTemplateChild("BackButtonHolder") is not FrameworkElement backButtonHolder
            || Window?.AppWindow is null
            || InputNonClientPointerSource.GetForWindowId(Window.AppWindow.Id) is not InputNonClientPointerSource nonClientSource)
            return;

        // Get the raw non-DPI aware rect that represents the region of the back button container.
        var transform = backButtonHolder.TransformToVisual(null);
        var bounds = transform.TransformBounds(new Rect(0, 0, backButtonHolder.ActualWidth, backButtonHolder.ActualHeight));

        // Calculate the DPI aware version of the region.
        var rasterizationScale = XamlRoot?.RasterizationScale ?? 1;
        var backButtonRect = new RectInt32((int)Math.Round(bounds.X * rasterizationScale),
            (int)Math.Round(bounds.Y * rasterizationScale),
            (int)Math.Round(bounds.Width * rasterizationScale),
            (int)Math.Round(bounds.Height * rasterizationScale));

        // Set that region to passthrough so the backbutton can be clicked.
        nonClientSource.SetRegionRects(NonClientRegionKind.Passthrough, [backButtonRect]);
    }

    private void TitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= TitleBar_Loaded;

        UpdateTitleBarLayout();
        UpdateClickThroughRegions();

        XamlRoot.Changed += XamlRoot_Changed;

        if (GetTemplateChild("BackButton") is Button backButton)
            backButton.Click += BackButton_Click;

        if (GetTemplateChild("BackButtonHolder") is FrameworkElement backButtonHolder)
            backButtonHolder.SizeChanged += BackButtonHolder_SizeChanged;
    }

    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateTitleBarLayout();

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        var raiseEvent = BackRequested;
        raiseEvent?.Invoke(this, new());
    }

    private void BackButtonHolder_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateClickThroughRegions();

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
            Foreground = (Brush)App.Current.Resources["WindowCaptionForegroundDisabled"];
        else
            Foreground = (Brush)App.Current.Resources["WindowCaptionForeground"];
    }
}
