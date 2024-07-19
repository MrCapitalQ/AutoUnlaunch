using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Tests.Settings.Launchers.Steam;

public class SteamSettingsViewModelTests
{
    private readonly IApplicationDataStore _applicationDataStore;
    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    private readonly SteamSettingsViewModel _viewModel;

    public SteamSettingsViewModelTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();
        _messenger = Substitute.For<IMessenger>();
        _protocolLauncher = Substitute.For<IProtocolLauncher>();

        _viewModel = new(new SteamSettingsService(_applicationDataStore), _messenger, _protocolLauncher);
    }

    [Fact]
    public void Ctor_WithSavedSettings_InitializesFromSettings()
    {
        var expectedIsEnabled = false;
        _applicationDataStore.GetValue("Steam_IsEnabled").Returns(expectedIsEnabled);
        var expectedDelay = 15;
        _applicationDataStore.GetValue("Steam_StopDelay").Returns(expectedDelay);
        var expectedStopMethod = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValue("Steam_StopMethod").Returns((int)expectedStopMethod);
        var expectedHideSetting = true;
        _applicationDataStore.GetValue("Steam_HidesShutdownScreen").Returns(expectedHideSetting);
        _applicationDataStore.GetValue("Steam_HidesOnActivityStart").Returns(expectedHideSetting);
        _applicationDataStore.GetValue("Steam_HidesOnActivityEnd").Returns(expectedHideSetting);
        var expectedstartMenuSetting = true;
        _applicationDataStore.GetValue("Steam_ShowUnnestedInStartMenu").Returns(expectedstartMenuSetting);

        var viewModel = new SteamSettingsViewModel(new SteamSettingsService(_applicationDataStore),
            _messenger,
            _protocolLauncher);

        Assert.Equal(expectedIsEnabled, viewModel.IsEnabled);
        Assert.Equal(expectedDelay, viewModel.SelectedDelay.Value);
        Assert.Equal(expectedStopMethod, viewModel.SelectedStopMethod.Value);
        Assert.Equal(expectedHideSetting, viewModel.HidesShutdownScreen);
        Assert.Equal(expectedHideSetting, viewModel.HidesOnActivityStart);
        Assert.Equal(expectedHideSetting, viewModel.HidesOnActivityEnd);
        Assert.Equal(expectedstartMenuSetting, viewModel.ShowUnnestedInStartMenu);
    }

    [Fact]
    public void Ctor_NoSavedSettings_InitializesWithDefaults()
    {
        var viewModel = new SteamSettingsViewModel(new SteamSettingsService(_applicationDataStore),
            _messenger,
            _protocolLauncher);

        Assert.True(viewModel.IsEnabled);
        Assert.Equal(5, viewModel.SelectedDelay.Value);
        Assert.Equal(LauncherStopMethod.RequestShutdown, viewModel.SelectedStopMethod.Value);
        Assert.False(viewModel.HidesShutdownScreen);
        Assert.False(viewModel.HidesOnActivityStart);
        Assert.False(viewModel.HidesOnActivityEnd);
        Assert.False(viewModel.ShowUnnestedInStartMenu);
    }

    [Fact]
    public void GetDelayOptions_ReturnsOptions()
    {
        List<int> expected = [0, 5, 15, 30, 60];

        var actual = _viewModel.DelayOptions;

        Assert.Equivalent(expected, actual.Select(x => x.Value));
    }

    [Fact]
    public void GetStopMethodOptions_ReturnsApplicableOptions()
    {
        List<LauncherStopMethod> expected = [LauncherStopMethod.RequestShutdown, LauncherStopMethod.KillProcess];

        var actual = _viewModel.StopMethodOptions;

        Assert.Equivalent(expected, actual.Select(x => x.Value));
    }

    [Fact]
    public void SetIsEnabled_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = false;

        _viewModel.IsEnabled = expected;

        Assert.Equal(expected, _viewModel.IsEnabled);
        _applicationDataStore.Received(1).SetValue("Steam_IsEnabled", expected);
    }

    [Fact]
    public void SetSelectedDelay_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = _viewModel.DelayOptions.First(x => x.Value == 15);

        _viewModel.SelectedDelay = expected;

        Assert.Equal(expected, _viewModel.SelectedDelay);
        _applicationDataStore.Received(1).SetValue("Steam_StopDelay", expected.Value);
    }

    [Fact]
    public void SetSelectedStopMethod_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = _viewModel.StopMethodOptions.First(x => x.Value == LauncherStopMethod.KillProcess);

        _viewModel.SelectedStopMethod = expected;

        Assert.Equal(expected, _viewModel.SelectedStopMethod);
        _applicationDataStore.Received(1).SetValue("Steam_StopMethod", (int)expected.Value);
    }

    [Fact]
    public void SetHidesShutdownScreen_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = true;

        _viewModel.HidesShutdownScreen = expected;

        Assert.Equal(expected, _viewModel.IsEnabled);
        _applicationDataStore.Received(1).SetValue("Steam_HidesShutdownScreen", expected);
    }

    [Fact]
    public void SetHidesOnActivityStart_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = true;

        _viewModel.HidesOnActivityStart = expected;

        Assert.Equal(expected, _viewModel.IsEnabled);
        _applicationDataStore.Received(1).SetValue("Steam_HidesOnActivityStart", expected);
    }

    [Fact]
    public void SetHidesOnActivityEnd_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = true;

        _viewModel.HidesOnActivityEnd = expected;

        Assert.Equal(expected, _viewModel.IsEnabled);
        _applicationDataStore.Received(1).SetValue("Steam_HidesOnActivityEnd", expected);
    }

    [Fact]
    public void SetShowUnnestedInStartMenu_SavesSettingAndSendsSteamStartMenuSettingsChangedMessage()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = true;

        _viewModel.ShowUnnestedInStartMenu = expected;

        Assert.Equal(expected, _viewModel.IsEnabled);
        _applicationDataStore.Received(1).SetValue("Steam_ShowUnnestedInStartMenu", expected);
        _messenger.Received(1).Send(SteamStartMenuSettingsChangedMessage.Instance, Arg.Any<TestMessengerToken>());
    }

    [Fact]
    public void MoreCommand_SendsNavigateMessage()
    {
        var navigateMessage = new SlideNavigateMessage(typeof(SteamSettingsPage), SlideNavigationTransitionEffect.FromRight);

        _viewModel.MoreCommand.Execute(null);

        _messenger.Received(1).Send<NavigateMessage, TestMessengerToken>(navigateMessage, Arg.Any<TestMessengerToken>());
    }

    [Fact]
    public async Task OpenSteamSettingsCommand_SendsLaunchesUriProtocol()
    {
        await _viewModel.OpenSteamSettingsCommand.ExecuteAsync(null);

        await _protocolLauncher.Received(1).LaunchUriAsync(new Uri("steam://settings/interface"));
    }
}
