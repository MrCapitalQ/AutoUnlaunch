using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class SettingsServiceTests
{
    private const string HasBeenLaunchedOnceKey = "HasBeenLaunchedOnce";
    private const string AppExitBehaviorKey = "AppExitBehavior";
    private const string MinimumLogLevelKey = "MinimumLogLevel";

    private readonly IApplicationDataStore _applicationDataStore;

    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();

        _settingsService = new(_applicationDataStore);
    }

    [Fact]
    public void GetHasBeenLaunchedOnce_DataStoreReturnsBool_ReturnsValue()
    {
        _applicationDataStore.GetValue(HasBeenLaunchedOnceKey).Returns(true);

        var actual = _settingsService.GetHasBeenLaunchedOnce();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(HasBeenLaunchedOnceKey);
    }

    [Fact]
    public void GetHasBeenLaunchedOnce_DataStoreReturnsNull_ReturnsFalse()
    {
        _applicationDataStore.GetValue(HasBeenLaunchedOnceKey).Returns(null);

        var actual = _settingsService.GetHasBeenLaunchedOnce();

        Assert.False(actual);
        _applicationDataStore.Received(1).GetValue(HasBeenLaunchedOnceKey);
    }

    [Fact]
    public void SetHasBeenLaunchedOnce_SavesTrueValueInApplicationDataStore()
    {
        _settingsService.SetHasBeenLaunchedOnce();

        _applicationDataStore.Received(1).SetValue(HasBeenLaunchedOnceKey, true);
    }

    [Fact]
    public void GetAppExitBehavior_ReturnsValueFromApplicationDataStore()
    {
        var expected = AppExitBehavior.Stop;
        _applicationDataStore.GetValueOrDefault(AppExitBehaviorKey, Arg.Any<int>()).Returns((int)expected);

        var actual = _settingsService.GetAppExitBehavior();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(AppExitBehaviorKey, (int)AppExitBehavior.RunInBackground);
    }

    [Fact]
    public void SetAppExitBehavior_SavesValueInApplicationDataStore()
    {
        var value = AppExitBehavior.Stop;

        _settingsService.SetAppExitBehavior(value);

        _applicationDataStore.Received(1).SetValue(AppExitBehaviorKey, (int)value);
    }

    [Fact]
    public void GetMinimumLogLevel_DataStoreReturnsInt_ReturnsEnumValue()
    {
        var expected = LogLevel.Debug;
        _applicationDataStore.GetValueOrDefault(MinimumLogLevelKey, Arg.Any<int>()).Returns((int)expected);

        var actual = _settingsService.GetMinimumLogLevel();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(MinimumLogLevelKey, (int)LogLevel.Information);
    }

    [Fact]
    public void SetMinimumLogLevel_SavesValueInApplicationDataStore()
    {
        var value = LogLevel.Debug;

        _settingsService.SetMinimumLogLevel(value);

        _applicationDataStore.Received(1).SetValue(MinimumLogLevelKey, (int)value);
    }
}
