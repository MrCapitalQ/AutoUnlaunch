using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;

internal partial class SteamSettingsViewModel : LauncherSettingsViewModel, ISteamSettingsViewModel
{
    private static readonly List<LauncherStopMethod> s_allowedStopOptions =
    [
        LauncherStopMethod.RequestShutdown,
        LauncherStopMethod.KillProcess
    ];
    private static readonly IEnumerable<ComboBoxOption<LauncherStopMethod>> s_stopMethodOptions = s_allowedStopOptions
        .Select(x => new ComboBoxOption<LauncherStopMethod>(x, s_stopMethodDisplayStrings[x]))
        .ToList();
    private static readonly Uri s_interfaceSettingsUri = new($"{LauncherUriProtocols.Steam}settings/interface");

    private readonly SteamSettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    [ObservableProperty]
    private bool _hidesShutdownScreen;

    [ObservableProperty]
    private bool _hidesOnActivityStart;

    [ObservableProperty]
    private bool _hidesOnActivityEnd;

    public SteamSettingsViewModel(SteamSettingsService settingsService,
        IMessenger messenger,
        IProtocolLauncher protocolLauncher)
        : base(settingsService, LauncherStopMethod.RequestShutdown)
    {
        _settingsService = settingsService;
        _messenger = messenger;
        _protocolLauncher = protocolLauncher;

        HidesShutdownScreen = _settingsService.GetHidesShutdownScreen() ?? false;
        HidesOnActivityStart = _settingsService.GetHidesOnActivityStart() ?? false;
        HidesOnActivityEnd = _settingsService.GetHidesOnActivityEnd() ?? false;
    }

    public override IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions => s_stopMethodOptions;

    [RelayCommand]
    private void More() => _messenger.Send<NavigateMessage>(new SlideNavigateMessage(typeof(SteamSettingsPage),
        SlideNavigationTransitionEffect.FromRight));

    [RelayCommand]
    private async Task OpenSteamSettingsAsync() => await _protocolLauncher.LaunchUriAsync(s_interfaceSettingsUri);

    partial void OnHidesShutdownScreenChanged(bool value) => _settingsService.SetHidesShutdownScreen(value);

    partial void OnHidesOnActivityStartChanged(bool value) => _settingsService.SetHidesOnActivityStart(value);

    partial void OnHidesOnActivityEndChanged(bool value) => _settingsService.SetHidesOnActivityEnd(value);
}
