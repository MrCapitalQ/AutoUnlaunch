using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Shared;
using Windows.ApplicationModel;
using WinUIEx;

namespace MrCapitalQ.AutoUnlaunch;

public sealed partial class MainWindow : WindowEx
{
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly ContentDialog _closeDialog = new()
    {
        Title = "Run in the background?",
        Content = "This app needs run while it's closed in order to work properly. Continue running in the background?",
        PrimaryButtonText = "Yes",
        SecondaryButtonText = "No",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary
    };

    private bool _isDialogVisible = false;

    public MainWindow(ISettingsService settingsService, IMessenger messenger)
    {
        _settingsService = settingsService;
        _messenger = messenger;

        InitializeComponent();

        Title = Package.Current.DisplayName;
        ExtendsContentIntoTitleBar = true;
        PersistenceId = nameof(MainWindow);
        AppWindow.Closing += AppWindow_Closing;
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
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

    private async Task<ContentDialogResult> ShowCloseDialog()
    {
        if (_isDialogVisible)
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
}
