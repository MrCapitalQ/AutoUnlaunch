using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Settings;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;
using Windows.ApplicationModel;
using WinUIEx;

namespace MrCapitalQ.AutoUnlaunch;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class MainWindow : WindowEx
{
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly ILogger<MainWindow> _logger;

    private readonly ContentDialog _closeDialog = new()
    {
        Title = "Run in the background?",
        Content = "This app needs run while it's closed in order to work properly. Continue running in the background?",
        PrimaryButtonText = "Yes",
        SecondaryButtonText = "No",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
    };

    private bool _isDialogVisible = false;

    public MainWindow(ISettingsService settingsService, IMessenger messenger, ILogger<MainWindow> logger)
    {
        _settingsService = settingsService;
        _messenger = messenger;
        _logger = logger;

        InitializeComponent();

        Title = Package.Current.DisplayName;
        ExtendsContentIntoTitleBar = true;
        PersistenceId = nameof(MainWindow);
        AppWindow.Closing += AppWindow_Closing;

        RootFrame.Navigated += RootFrame_Navigated;
        RootFrame.Navigate(typeof(SettingsPage));

        messenger.Register<MainWindow, NavigateMessage>(this, (r, m) =>
        {
            NavigationTransitionInfo? transitionInfo = m switch
            {
                SlideNavigateMessage slideNavigateMessage => new SlideNavigationTransitionInfo { Effect = slideNavigateMessage.SlideEffect },
                _ => null
            };
            r.RootFrame.Navigate(m.SourcePageType, m.Parameter, transitionInfo);
        });

        messenger.Register<MainWindow, ShowDialogMessage>(this, async (r, m) =>
        {
            if (r.Content.XamlRoot is null)
                return;

            var dialog = new ContentDialog()
            {
                Title = m.Title,
                Content = m.Message,
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = r.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
            };

            await dialog.ShowAsync();
        });
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            var appExitBehavior = _settingsService.GetAppExitBehavior();
            if (appExitBehavior == AppExitBehavior.RunInBackground)
                return;

            if (appExitBehavior == AppExitBehavior.Stop)
            {
                _messenger.Send(new StopAppMessage());
                return;
            }

            args.Cancel = true;

            var result = await ShowCloseDialog();
            if (result == ContentDialogResult.None)
                return;

            if (result == ContentDialogResult.Secondary)
                _messenger.Send(new StopAppMessage());

            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when handling main window closing event.");
        }
    }

    private async Task<ContentDialogResult> ShowCloseDialog()
    {
        if (_isDialogVisible || Content.XamlRoot is null)
            return ContentDialogResult.None;

        _isDialogVisible = true;
        try
        {
            _closeDialog.XamlRoot = Content.XamlRoot;
            var result = await _closeDialog.ShowAsync();
            return result;
        }
        finally
        {
            _isDialogVisible = false;
        }
    }

    public string Icon => "Assets/AppIcon.ico";

    private void GoBack()
    {
        if (RootFrame.CanGoBack)
            RootFrame.GoBack();
    }

    private void GoForward()
    {
        if (RootFrame.CanGoForward)
            RootFrame.GoForward();
    }

    private void TitleBar_BackRequested(object sender, EventArgs e) => GoBack();

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerProperties = e.GetCurrentPoint(sender as UIElement).Properties;
        if (pointerProperties.IsXButton1Pressed)
        {
            GoBack();
            e.Handled = true;
        }
        else if (pointerProperties.IsXButton2Pressed)
        {
            GoForward();
            e.Handled = true;
        }
    }

    private void BackKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        GoBack();
        args.Handled = true;
    }

    private void ForwardKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        GoForward();
        args.Handled = true;
    }

    private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        => TitleBar.IsBackButtonVisible = RootFrame.CanGoBack;
}
