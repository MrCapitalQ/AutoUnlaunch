﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Gog;

internal partial class GogSettingsViewModel : LauncherSettingsViewModel, IGogSettingsViewModel
{
    private static readonly List<LauncherStopMethod> s_allowedStopOptions =
    [
        LauncherStopMethod.RequestShutdown,
        LauncherStopMethod.KillProcess
    ];
    private static readonly IEnumerable<ComboBoxOption<LauncherStopMethod>> s_stopMethodOptions = s_allowedStopOptions
        .Select(x => new ComboBoxOption<LauncherStopMethod>(x, s_stopMethodDisplayStrings[x]))
        .ToList();
    private static readonly Uri s_launchUri = new(LauncherUriProtocols.Gog);

    private readonly GogSettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    [ObservableProperty]
    private bool _hidesOnActivityEnd;

    public GogSettingsViewModel(GogSettingsService settingsService,
        IMessenger messenger,
        IProtocolLauncher protocolLauncher)
        : base(settingsService)
    {
        _settingsService = settingsService;
        _messenger = messenger;
        _protocolLauncher = protocolLauncher;

        _hidesOnActivityEnd = _settingsService.GetHidesOnActivityEnd();
    }

    public override IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions => s_stopMethodOptions;

    [RelayCommand]
    private void More() => _messenger.Send<NavigateMessage>(new SlideNavigateMessage(typeof(GogSettingsPage), SlideNavigationTransitionEffect.FromRight));

    [RelayCommand]
    private async Task OpenGogGalaxyAsync() => await _protocolLauncher.LaunchUriAsync(s_launchUri);

    partial void OnHidesOnActivityEndChanged(bool value) => _settingsService.SetHidesOnActivityEnd(value);
}
