using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

public class SteamSettingsServiceTests
{
    private const string IsLauncherEnabledKey = "Steam_IsEnabled";
    private const string LauncherStopDelayTestKey = "Steam_StopDelay";
    private const string LauncherStopMethodTestKey = "Steam_StopMethod";
    private const string HidesShutdownScreenKey = "Steam_HidesShutdownScreen";
    private const string HidesOnActivityStartKey = "Steam_HidesOnActivityStart";
    private const string HidesOnActivityEndKey = "Steam_HidesOnActivityEnd";
    private const string ShowUnnestedInStartMenuKey = "Steam_ShowUnnestedInStartMenu";
    private readonly IApplicationDataStore _applicationDataStore;

    private readonly SteamSettingsService _steamSettingsService;

    public SteamSettingsServiceTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();

        _steamSettingsService = new(_applicationDataStore);
    }

    [Fact]
    public void GetIsLauncherEnabled_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(IsLauncherEnabledKey).Returns(true);

        var actual = _steamSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(IsLauncherEnabledKey);
    }

    [Fact]
    public void GetIsLauncherEnabled_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(IsLauncherEnabledKey).Returns(null);

        var actual = _steamSettingsService.GetIsLauncherEnabled();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(IsLauncherEnabledKey);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsValue_ReturnsValue()
    {
        var expected = 5;
        _applicationDataStore.GetValue(LauncherStopDelayTestKey).Returns(expected);

        var actual = _steamSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopDelayTestKey);
    }

    [Fact]
    public void GetLauncherStopDelay_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(LauncherStopDelayTestKey).Returns(null);

        var actual = _steamSettingsService.GetLauncherStopDelay();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopDelayTestKey);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _steamSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsIntValue_ReturnsLauncherStopMethodValue()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValue(LauncherStopMethodTestKey).Returns((int)expected);

        var actual = _steamSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopMethodTestKey);
    }

    [Fact]
    public void GetLauncherStopMethod_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(LauncherStopMethodTestKey).Returns(null);

        var actual = _steamSettingsService.GetLauncherStopMethod();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(LauncherStopMethodTestKey);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _steamSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }

    [Fact]
    public void GetHidesShutdownScreen_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(HidesShutdownScreenKey).Returns(true);

        var actual = _steamSettingsService.GetHidesShutdownScreen();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(HidesShutdownScreenKey);
    }

    [Fact]
    public void GetHidesShutdownScreen_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(HidesShutdownScreenKey).Returns(null);

        var actual = _steamSettingsService.GetHidesShutdownScreen();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(HidesShutdownScreenKey);
    }

    [Fact]
    public void SetHidesShutdownScreen_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetHidesShutdownScreen(value);

        _applicationDataStore.Received(1).SetValue(HidesShutdownScreenKey, value);
    }

    [Fact]
    public void GetHidesOnActivityStart_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(HidesOnActivityStartKey).Returns(true);

        var actual = _steamSettingsService.GetHidesOnActivityStart();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(HidesOnActivityStartKey);
    }

    [Fact]
    public void GetHidesOnActivityStart_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(HidesOnActivityStartKey).Returns(null);

        var actual = _steamSettingsService.GetHidesOnActivityStart();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(HidesOnActivityStartKey);
    }

    [Fact]
    public void SetHidesOnActivityStart_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetHidesOnActivityStart(value);

        _applicationDataStore.Received(1).SetValue(HidesOnActivityStartKey, value);
    }

    [Fact]
    public void GetHidesOnActivityEnd_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(HidesOnActivityEndKey).Returns(true);

        var actual = _steamSettingsService.GetHidesOnActivityEnd();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(HidesOnActivityEndKey);
    }

    [Fact]
    public void GetHidesOnActivityEnd_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(HidesOnActivityEndKey).Returns(null);

        var actual = _steamSettingsService.GetHidesOnActivityEnd();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(HidesOnActivityEndKey);
    }

    [Fact]
    public void SetHidesOnActivityEnd_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetHidesOnActivityEnd(value);

        _applicationDataStore.Received(1).SetValue(HidesOnActivityEndKey, value);
    }

    [Fact]
    public void GetShowUnnestedInStartMenu_DataStoreReturnsValue_ReturnsValue()
    {
        _applicationDataStore.GetValue(ShowUnnestedInStartMenuKey).Returns(true);

        var actual = _steamSettingsService.GetShowUnnestedInStartMenu();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValue(ShowUnnestedInStartMenuKey);
    }

    [Fact]
    public void GetShowUnnestedInStartMenu_DataStoreReturnsNull_ReturnsNull()
    {
        _applicationDataStore.GetValue(ShowUnnestedInStartMenuKey).Returns(null);

        var actual = _steamSettingsService.GetShowUnnestedInStartMenu();

        Assert.Null(actual);
        _applicationDataStore.Received(1).GetValue(ShowUnnestedInStartMenuKey);
    }

    [Fact]
    public void SetShowUnnestedInStartMenu_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetShowUnnestedInStartMenu(value);

        _applicationDataStore.Received(1).SetValue(ShowUnnestedInStartMenuKey, value);
    }
}
