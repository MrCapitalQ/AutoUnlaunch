using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class LauncherSettingsServiceTests
{
    private readonly IApplicationDataStore _applicationDataStore;

    private readonly TestLauncherSettingsService _launcherSettingsService;

    public LauncherSettingsServiceTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();

        _launcherSettingsService = new(_applicationDataStore);
    }

    [Fact]
    public void GetIsLauncherEnabled_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(TestLauncherSettingsService.IsLauncherEnabledTestKey).Returns(true);

        var actual = _launcherSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(TestLauncherSettingsService.IsLauncherEnabledTestKey);
    }

    [Fact]
    public void GetIsLauncherEnabled_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(TestLauncherSettingsService.IsLauncherEnabledTestKey).Returns(null);

        var actual = _launcherSettingsService.GetIsLauncherEnabled();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(TestLauncherSettingsService.IsLauncherEnabledTestKey);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _launcherSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(TestLauncherSettingsService.IsLauncherEnabledTestKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsValue_ReturnsValue()
    {
        var expected = 5;
        _applicationDataStore.GetValue(TestLauncherSettingsService.LauncherStopDelayTestKey).Returns(expected);

        var actual = _launcherSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(TestLauncherSettingsService.LauncherStopDelayTestKey);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(TestLauncherSettingsService.LauncherStopDelayTestKey).Returns(null);

        var actual = _launcherSettingsService.GetLauncherStopDelay();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(TestLauncherSettingsService.LauncherStopDelayTestKey);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _launcherSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(TestLauncherSettingsService.LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsIntValue_ReturnsLauncherStopMethodValue()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValue(TestLauncherSettingsService.LauncherStopMethodTestKey).Returns((int)expected);

        var actual = _launcherSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(TestLauncherSettingsService.LauncherStopMethodTestKey);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(TestLauncherSettingsService.LauncherStopMethodTestKey).Returns(null);

        var actual = _launcherSettingsService.GetLauncherStopMethod();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(TestLauncherSettingsService.LauncherStopMethodTestKey);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _launcherSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(TestLauncherSettingsService.LauncherStopMethodTestKey, (int)value);
    }
}
