using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.EA;

internal partial class EASettingsViewModel : LauncherSettingsViewModel, IEASettingsViewModel
{
    private static readonly List<LauncherStopMethod> s_allowedStopOptions =
    [
        LauncherStopMethod.CloseMainWindow,
        LauncherStopMethod.KillProcess
    ];
    private static readonly IEnumerable<ComboBoxOption<LauncherStopMethod>> s_stopMethodOptions = s_allowedStopOptions
        .Select(x => new ComboBoxOption<LauncherStopMethod>(x, s_stopMethodDisplayStrings[x]))
        .ToList();
    private static readonly Uri s_launchUri = new(LauncherUriProtocols.EA);

    private readonly EASettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    [ObservableProperty]
    private bool _minimizesOnActivityEnd;

    public EASettingsViewModel(EASettingsService settingsService,
        IMessenger messenger,
        IProtocolLauncher protocolLauncher)
        : base(settingsService, LauncherStopMethod.CloseMainWindow)
    {
        _settingsService = settingsService;
        _messenger = messenger;
        _protocolLauncher = protocolLauncher;

        _minimizesOnActivityEnd = _settingsService.GetMinimizesOnActivityEnd() ?? false;
    }

    public override IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions => s_stopMethodOptions;

    [RelayCommand]
    private void More() => _messenger.Send<NavigateMessage>(new SlideNavigateMessage(typeof(EASettingsPage), SlideNavigationTransitionEffect.FromRight));

    [RelayCommand]
    private async Task OpenEAAsync() => await _protocolLauncher.LaunchUriAsync(s_launchUri);

    partial void OnMinimizesOnActivityEndChanged(bool value) => _settingsService.SetMinimizesOnActivityEnd(value);
}

