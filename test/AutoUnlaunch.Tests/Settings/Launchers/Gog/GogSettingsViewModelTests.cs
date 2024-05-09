using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Gog;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Tests.Settings.Launchers.Gog;

public class GogSettingsViewModelTests
{
    private readonly IApplicationDataStore _applicationDataStore;
    private readonly IMessenger _messenger;
    private readonly IProtocolLauncher _protocolLauncher;

    private readonly GogSettingsViewModel _viewModel;

    public GogSettingsViewModelTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();
        _messenger = Substitute.For<IMessenger>();
        _protocolLauncher = Substitute.For<IProtocolLauncher>();

        _viewModel = new(new GogSettingsService(_applicationDataStore), _messenger, _protocolLauncher);
    }

    [Fact]
    public void Ctor_WithSavedSettings_InitializesFromSettings()
    {
        var expectedIsEnabled = false;
        _applicationDataStore.GetValue("GOG_IsEnabled").Returns(expectedIsEnabled);
        var expectedDelay = 15;
        _applicationDataStore.GetValue("GOG_StopDelay").Returns(expectedDelay);
        var expectedStopMethod = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValue("GOG_StopMethod").Returns((int)expectedStopMethod);
        var expectedMinimizeSetting = true;
        _applicationDataStore.GetValue("GOG_MinimizesOnActivityEnd").Returns(expectedMinimizeSetting);

        var viewModel = new GogSettingsViewModel(new GogSettingsService(_applicationDataStore),
            _messenger,
            _protocolLauncher);

        Assert.Equal(expectedIsEnabled, viewModel.IsEnabled);
        Assert.Equal(expectedDelay, viewModel.SelectedDelay.Value);
        Assert.Equal(expectedStopMethod, viewModel.SelectedStopMethod.Value);
    }

    [Fact]
    public void Ctor_NoSavedSettings_InitializesWithDefaults()
    {
        var viewModel = new GogSettingsViewModel(new GogSettingsService(_applicationDataStore),
            _messenger,
            _protocolLauncher);

        Assert.True(viewModel.IsEnabled);
        Assert.Equal(5, viewModel.SelectedDelay.Value);
        Assert.Equal(LauncherStopMethod.RequestShutdown, viewModel.SelectedStopMethod.Value);
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
        _applicationDataStore.Received(1).SetValue("GOG_IsEnabled", expected);
    }

    [Fact]
    public void SetSelectedDelay_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = _viewModel.DelayOptions.First(x => x.Value == 15);

        _viewModel.SelectedDelay = expected;

        Assert.Equal(expected, _viewModel.SelectedDelay);
        _applicationDataStore.Received(1).SetValue("GOG_StopDelay", expected.Value);
    }

    [Fact]
    public void SetSelectedStopMethod_SavesSetting()
    {
        _applicationDataStore.ClearReceivedCalls();
        var expected = _viewModel.StopMethodOptions.First(x => x.Value == LauncherStopMethod.KillProcess);

        _viewModel.SelectedStopMethod = expected;

        Assert.Equal(expected, _viewModel.SelectedStopMethod);
        _applicationDataStore.Received(1).SetValue("GOG_StopMethod", (int)expected.Value);
    }

    [Fact]
    public void MoreCommand_SendsNavigateMessage()
    {
        var navigateMessage = new SlideNavigateMessage(typeof(GogSettingsPage), SlideNavigationTransitionEffect.FromRight);

        _viewModel.MoreCommand.Execute(null);

        _messenger.Received(1).Send<NavigateMessage, TestMessengerToken>(navigateMessage, Arg.Any<TestMessengerToken>());
    }

    [Fact]
    public async Task OpenGogGalaxyCommand_SendsLaunchesUriProtocol()
    {
        await _viewModel.OpenGogGalaxyCommand.ExecuteAsync(null);

        await _protocolLauncher.Received(1).LaunchUriAsync(new Uri("goggalaxy://"));
    }
}
