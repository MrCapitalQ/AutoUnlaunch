using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class SettingsServiceTests
{
    private const string HasBeenLaunchedOnceKey = "HasBeenLaunchedOnce";
    private const string AppExitBehaviorKey = "AppExitBehavior";
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
    public void GetAppExitBehavior_DataStoreReturnsInt_ReturnsEnumValue()
    {
        var expected = AppExitBehavior.Stop;
        _applicationDataStore.GetValue(AppExitBehaviorKey).Returns((int)expected);

        var actual = _settingsService.GetAppExitBehavior();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(AppExitBehaviorKey);
    }

    [Fact]
    public void GetAppExitBehavior_DataStoreReturnsNull_ReturnsRunInBackground()
    {
        _applicationDataStore.GetValue(AppExitBehaviorKey).Returns(null);

        var actual = _settingsService.GetAppExitBehavior();

        Assert.Equal(AppExitBehavior.RunInBackground, actual);
        _applicationDataStore.Received(1).GetValue(AppExitBehaviorKey);
    }

    [Fact]
    public void SetAppExitBehavior_SavesTrueValueInApplicationDataStore()
    {
        var value = AppExitBehavior.Stop;

        _settingsService.SetAppExitBehavior(value);

        _applicationDataStore.Received(1).SetValue(AppExitBehaviorKey, (int)value);
    }
}
