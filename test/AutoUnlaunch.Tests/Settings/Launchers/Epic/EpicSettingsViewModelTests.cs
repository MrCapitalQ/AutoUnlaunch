using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Epic;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Tests.Settings.Launchers.Epic;


public class EpicSettingsViewModelTests
{
    private readonly IApplicationDataStore _applicationDataStore;
    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    private readonly EpicSettingsViewModel _viewModel;

    public EpicSettingsViewModelTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();
        _messenger = Substitute.For<IMessenger>();
        _protocolLauncher = Substitute.For<IProtocolLauncher>();

        _applicationDataStore.GetValueOrDefault("Epic_IsEnabled", Arg.Any<bool>()).Returns(true);
        _applicationDataStore.GetValueOrDefault("Epic_StopDelay", Arg.Any<int>()).Returns(5);
        _applicationDataStore.GetValueOrDefault("Epic_StopMethod", Arg.Any<int>()).Returns((int)LauncherStopMethod.CloseMainWindow);

        _viewModel = new(new EpicSettingsService(_applicationDataStore), _messenger, _protocolLauncher);
    }

    [Fact]
    public void Ctor_InitializesFromSettings()
    {
        var expectedIsEnabled = false;
        _applicationDataStore.GetValueOrDefault("Epic_IsEnabled", Arg.Any<bool>()).Returns(expectedIsEnabled);
        var expectedDelay = 15;
        _applicationDataStore.GetValueOrDefault("Epic_StopDelay", Arg.Any<int>()).Returns(expectedDelay);
        var expectedStopMethod = LauncherStopMethod.CloseMainWindow;
        _applicationDataStore.GetValueOrDefault("Epic_StopMethod", Arg.Any<int>()).Returns((int)expectedStopMethod);

        var viewModel = new EpicSettingsViewModel(new EpicSettingsService(_applicationDataStore),
            _messenger,
            _protocolLauncher);

        Assert.Equal(expectedIsEnabled, viewModel.IsEnabled);
        Assert.Equal(expectedDelay, viewModel.SelectedDelay.Value);
        Assert.Equal(expectedStopMethod, viewModel.SelectedStopMethod.Value);
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
        List<LauncherStopMethod> expected = [LauncherStopMethod.CloseMainWindow, LauncherStopMethod.KillProcess];

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
        _applicationDataStore.Received(1).SetValue("Epic_IsEnabled", expected);
    }

    [Fact]
    public void SetSelectedDelay_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = _viewModel.DelayOptions.First(x => x.Value == 15);

        _viewModel.SelectedDelay = expected;

        Assert.Equal(expected, _viewModel.SelectedDelay);
        _applicationDataStore.Received(1).SetValue("Epic_StopDelay", expected.Value);
    }

    [Fact]
    public void SetSelectedStopMethod_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = _viewModel.StopMethodOptions.First(x => x.Value == LauncherStopMethod.KillProcess);

        _viewModel.SelectedStopMethod = expected;

        Assert.Equal(expected, _viewModel.SelectedStopMethod);
        _applicationDataStore.Received(1).SetValue("Epic_StopMethod", (int)expected.Value);
    }

    [Fact]
    public void MoreCommand_SendsNavigateMessage()
    {
        var navigateMessage = new SlideNavigateMessage(typeof(EpicSettingsPage), SlideNavigationTransitionEffect.FromRight);

        _viewModel.MoreCommand.Execute(null);

        _messenger.Received(1).Send<NavigateMessage, TestMessengerToken>(navigateMessage, Arg.Any<TestMessengerToken>());
    }

    [Fact]
    public async Task OpenEpicGamesCommand_SendsLaunchesUriProtocol()
    {
        await _viewModel.OpenEpicGamesCommand.ExecuteAsync(null);

        await _protocolLauncher.Received(1).LaunchUriAsync(new Uri("com.epicgames.launcher://"));
    }
}
