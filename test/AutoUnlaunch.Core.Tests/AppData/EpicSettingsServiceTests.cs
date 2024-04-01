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
    public void GetIsLauncherEnabled_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(IsLauncherEnabledKey).Returns(true);

        var actual = _epicSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(IsLauncherEnabledKey);
    }

    [Fact]
    public void GetIsLauncherEnabled_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(IsLauncherEnabledKey).Returns(null);

        var actual = _epicSettingsService.GetIsLauncherEnabled();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(IsLauncherEnabledKey);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _epicSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsValue_ReturnsValue()
    {
        var expected = 5;
        _applicationDataStore.GetValue(LauncherStopDelayTestKey).Returns(expected);

        var actual = _epicSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopDelayTestKey);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(LauncherStopDelayTestKey).Returns(null);

        var actual = _epicSettingsService.GetLauncherStopDelay();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopDelayTestKey);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _epicSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsIntValue_ReturnsLauncherStopMethodValue()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValue(LauncherStopMethodTestKey).Returns((int)expected);

        var actual = _epicSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopMethodTestKey);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(LauncherStopMethodTestKey).Returns(null);

        var actual = _epicSettingsService.GetLauncherStopMethod();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopMethodTestKey);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _epicSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }
}
