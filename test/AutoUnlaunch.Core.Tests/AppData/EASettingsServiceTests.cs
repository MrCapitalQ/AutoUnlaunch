using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class EASettingsServiceTests
{
    private const string IsLauncherEnabledKey = "EA_IsEnabled";
    private const string LauncherStopDelayTestKey = "EA_StopDelay";
    private const string LauncherStopMethodTestKey = "EA_StopMethod";
    private const string MinimizesOnActivityEndKey = "EA_MinimizesOnActivityEnd";
    private readonly IApplicationDataStore _applicationDataStore;

    private readonly EASettingsService _eaSettingsService;

    public EASettingsServiceTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();

        _eaSettingsService = new(_applicationDataStore);
    }

    [Fact]
    public void GetIsLauncherEnabled_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(IsLauncherEnabledKey, Arg.Any<bool>()).Returns(true);

        var actual = _eaSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(IsLauncherEnabledKey, true);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _eaSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_ReturnsValueFromApplicationDataStore()
    {
        var expected = 5;
        _applicationDataStore.GetValueOrDefault(LauncherStopDelayTestKey, Arg.Any<int>()).Returns(expected);

        var actual = _eaSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopDelayTestKey, 5);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _eaSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_ReturnsValueFromApplicationDataStore()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValueOrDefault(LauncherStopMethodTestKey, Arg.Any<int>()).Returns((int)expected);

        var actual = _eaSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopMethodTestKey, (int)LauncherStopMethod.CloseMainWindow);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _eaSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }

    [Fact]
    public void GetMinimizesOnActivityStart_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(MinimizesOnActivityEndKey, Arg.Any<bool>()).Returns(true);

        var actual = _eaSettingsService.GetMinimizesOnActivityEnd();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(MinimizesOnActivityEndKey, false);
    }

    [Fact]
    public void SetMinimizesOnActivityStart_SavesValueInApplicationDataStore()
    {
        var value = true;

        _eaSettingsService.SetMinimizesOnActivityEnd(value);

        _applicationDataStore.Received(1).SetValue(MinimizesOnActivityEndKey, value);
    }
}
