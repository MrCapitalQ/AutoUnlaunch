using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class EpicSettingsServiceTests
{
    private const string IsLauncherEnabledKey = "Epic_IsEnabled";
    private const string LauncherStopDelayTestKey = "Epic_StopDelay";
    private const string LauncherStopMethodTestKey = "Epic_StopMethod";
    private readonly IApplicationDataStore _applicationDataStore;

    private readonly EpicSettingsService _epicSettingsService;

    public EpicSettingsServiceTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();

        _epicSettingsService = new(_applicationDataStore);
    }

    [Fact]
    public void GetIsLauncherEnabled_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(IsLauncherEnabledKey, Arg.Any<bool>()).Returns(true);

        var actual = _epicSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(IsLauncherEnabledKey, true);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _epicSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_ReturnsValueFromApplicationDataStore()
    {
        var expected = 5;
        _applicationDataStore.GetValueOrDefault(LauncherStopDelayTestKey, Arg.Any<int>()).Returns(expected);

        var actual = _epicSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopDelayTestKey, 5);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _epicSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_ReturnsValueFromApplicationDataStore()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValueOrDefault(LauncherStopMethodTestKey, Arg.Any<int>()).Returns((int)expected);

        var actual = _epicSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopMethodTestKey, (int)LauncherStopMethod.CloseMainWindow);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _epicSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }
}
