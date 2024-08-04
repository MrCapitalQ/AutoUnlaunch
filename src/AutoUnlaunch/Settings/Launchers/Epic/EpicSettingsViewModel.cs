using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Epic;

internal partial class EpicSettingsViewModel : LauncherSettingsViewModel, IEpicSettingsViewModel
{
    private static readonly List<LauncherStopMethod> s_allowedStopOptions =
    [
        LauncherStopMethod.CloseMainWindow,
        LauncherStopMethod.KillProcess
    ];
    private static readonly IEnumerable<ComboBoxOption<LauncherStopMethod>> s_stopMethodOptions = s_allowedStopOptions
        .Select(x => new ComboBoxOption<LauncherStopMethod>(x, s_stopMethodDisplayStrings[x]))
        .ToList();
    private static readonly Uri s_launchUri = new(LauncherUriProtocols.Epic);

    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    public EpicSettingsViewModel(EpicSettingsService settingsService,
        IMessenger messenger,
        IProtocolLauncher protocolLauncher)
        : base(settingsService)
    {
        _messenger = messenger;
        _protocolLauncher = protocolLauncher;
    }

    public override IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions => s_stopMethodOptions;

    [RelayCommand]
    private void More() => _messenger.Send<NavigateMessage>(new SlideNavigateMessage(typeof(EpicSettingsPage), SlideNavigationTransitionEffect.FromRight));

    [RelayCommand]
    private async Task OpenEpicGamesAsync() => await _protocolLauncher.LaunchUriAsync(s_launchUri);
}

