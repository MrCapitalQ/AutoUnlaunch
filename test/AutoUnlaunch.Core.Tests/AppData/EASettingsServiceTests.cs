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
    public void GetIsLauncherEnabled_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(IsLauncherEnabledKey).Returns(true);

        var actual = _eaSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(IsLauncherEnabledKey);
    }

    [Fact]
    public void GetIsLauncherEnabled_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(IsLauncherEnabledKey).Returns(null);

        var actual = _eaSettingsService.GetIsLauncherEnabled();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(IsLauncherEnabledKey);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _eaSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsValue_ReturnsValue()
    {
        var expected = 5;
        _applicationDataStore.GetValue(LauncherStopDelayTestKey).Returns(expected);

        var actual = _eaSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopDelayTestKey);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(LauncherStopDelayTestKey).Returns(null);

        var actual = _eaSettingsService.GetLauncherStopDelay();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopDelayTestKey);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _eaSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsIntValue_ReturnsLauncherStopMethodValue()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValue(LauncherStopMethodTestKey).Returns((int)expected);

        var actual = _eaSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopMethodTestKey);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(LauncherStopMethodTestKey).Returns(null);

        var actual = _eaSettingsService.GetLauncherStopMethod();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopMethodTestKey);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _eaSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }

    [Fact]
    public void GetMinimizesOnActivityStart_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(MinimizesOnActivityEndKey).Returns(true);

        var actual = _eaSettingsService.GetMinimizesOnActivityEnd();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(MinimizesOnActivityEndKey);
    }

    [Fact]
    public void GetMinimizesOnActivityStart_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(MinimizesOnActivityEndKey).Returns(null);

        var actual = _eaSettingsService.GetMinimizesOnActivityEnd();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(MinimizesOnActivityEndKey);
    }

    [Fact]
    public void SetMinimizesOnActivityStart_SavesValueInApplicationDataStore()
    {
        var value = true;

        _eaSettingsService.SetMinimizesOnActivityEnd(value);

        _applicationDataStore.Received(1).SetValue(MinimizesOnActivityEndKey, value);
    }
}
