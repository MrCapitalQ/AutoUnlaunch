using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class GogSettingsServiceTests
{
    private const string IsLauncherEnabledKey = "GOG_IsEnabled";
    private const string LauncherStopDelayTestKey = "GOG_StopDelay";
    private const string LauncherStopMethodTestKey = "GOG_StopMethod";
    private const string HidesOnActivityEndKey = "GOG_HidesOnActivityEnd";
    private readonly IApplicationDataStore _applicationDataStore;

    private readonly GogSettingsService _gogSettingsService;

    public GogSettingsServiceTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();

        _gogSettingsService = new(_applicationDataStore);
    }

    [Fact]
    public void GetIsLauncherEnabled_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(IsLauncherEnabledKey, Arg.Any<bool>()).Returns(true);

        var actual = _gogSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(IsLauncherEnabledKey, true);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _gogSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_ReturnsValueFromApplicationDataStore()
    {
        var expected = 5;
        _applicationDataStore.GetValueOrDefault(LauncherStopDelayTestKey, Arg.Any<int>()).Returns(expected);

        var actual = _gogSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopDelayTestKey, 5);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _gogSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_ReturnsValueFromApplicationDataStore()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValueOrDefault(LauncherStopMethodTestKey, Arg.Any<int>()).Returns((int)expected);

        var actual = _gogSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopMethodTestKey, (int)LauncherStopMethod.RequestShutdown);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _gogSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }

    [Fact]
    public void GetHidesOnActivityStart_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(HidesOnActivityEndKey, Arg.Any<bool>()).Returns(true);

        var actual = _gogSettingsService.GetHidesOnActivityEnd();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(HidesOnActivityEndKey, false);
    }

    [Fact]
    public void SetHidesOnActivityStart_SavesValueInApplicationDataStore()
    {
        var value = true;

        _gogSettingsService.SetHidesOnActivityEnd(value);

        _applicationDataStore.Received(1).SetValue(HidesOnActivityEndKey, value);
    }
}
