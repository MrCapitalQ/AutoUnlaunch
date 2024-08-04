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
    public void GetIsLauncherEnabled_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(IsLauncherEnabledKey, Arg.Any<bool>()).Returns(true);

        var actual = _steamSettingsService.GetIsLauncherEnabled();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(IsLauncherEnabledKey, true);
    }

    [Fact]
    public void SetIsLauncherEnabled_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetIsLauncherEnabled(value);

        _applicationDataStore.Received(1).SetValue(IsLauncherEnabledKey, value);
    }

    [Fact]
    public void GetLauncherStopDelay_ReturnsValueFromApplicationDataStore()
    {
        var expected = 5;
        _applicationDataStore.GetValueOrDefault(LauncherStopDelayTestKey, Arg.Any<int>()).Returns(expected);

        var actual = _steamSettingsService.GetLauncherStopDelay();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopDelayTestKey, 5);
    }

    [Fact]
    public void SetLauncherStopDelay_SavesValueInApplicationDataStore()
    {
        var value = 5;

        _steamSettingsService.SetLauncherStopDelay(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopDelayTestKey, value);
    }

    [Fact]
    public void GetLauncherStopMethod_ReturnsValueFromApplicationDataStore()
    {
        var expected = LauncherStopMethod.RequestShutdown;
        _applicationDataStore.GetValueOrDefault(LauncherStopMethodTestKey, Arg.Any<int>()).Returns((int)expected);

        var actual = _steamSettingsService.GetLauncherStopMethod();

        Assert.Equal(expected, actual);
        _applicationDataStore.Received(1).GetValueOrDefault(LauncherStopMethodTestKey, (int)LauncherStopMethod.RequestShutdown);
    }

    [Fact]
    public void SetLauncherStopMethod_SavesIntValueInApplicationDataStore()
    {
        var value = LauncherStopMethod.RequestShutdown;

        _steamSettingsService.SetLauncherStopMethod(value);

        _applicationDataStore.Received(1).SetValue(LauncherStopMethodTestKey, (int)value);
    }

    [Fact]
    public void GetHidesShutdownScreen_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(HidesShutdownScreenKey, Arg.Any<bool>()).Returns(true);

        var actual = _steamSettingsService.GetHidesShutdownScreen();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(HidesShutdownScreenKey, false);
    }

    [Fact]
    public void SetHidesShutdownScreen_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetHidesShutdownScreen(value);

        _applicationDataStore.Received(1).SetValue(HidesShutdownScreenKey, value);
    }

    [Fact]
    public void GetHidesOnActivityStart_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(HidesOnActivityStartKey, Arg.Any<bool>()).Returns(true);

        var actual = _steamSettingsService.GetHidesOnActivityStart();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(HidesOnActivityStartKey, false);
    }

    [Fact]
    public void SetHidesOnActivityStart_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetHidesOnActivityStart(value);

        _applicationDataStore.Received(1).SetValue(HidesOnActivityStartKey, value);
    }

    [Fact]
    public void GetHidesOnActivityEnd_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(HidesOnActivityEndKey, Arg.Any<bool>()).Returns(true);

        var actual = _steamSettingsService.GetHidesOnActivityEnd();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(HidesOnActivityEndKey, false);
    }

    [Fact]
    public void SetHidesOnActivityEnd_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetHidesOnActivityEnd(value);

        _applicationDataStore.Received(1).SetValue(HidesOnActivityEndKey, value);
    }

    [Fact]
    public void GetShowUnnestedInStartMenu_ReturnsValueFromApplicationDataStore()
    {
        _applicationDataStore.GetValueOrDefault(ShowUnnestedInStartMenuKey, Arg.Any<bool>()).Returns(true);

        var actual = _steamSettingsService.GetShowUnnestedInStartMenu();

        Assert.True(actual);
        _applicationDataStore.Received(1).GetValueOrDefault(ShowUnnestedInStartMenuKey, false);
    }

    [Fact]
    public void SetShowUnnestedInStartMenu_SavesValueInApplicationDataStore()
    {
        var value = true;

        _steamSettingsService.SetShowUnnestedInStartMenu(value);

        _applicationDataStore.Received(1).SetValue(ShowUnnestedInStartMenuKey, value);
    }
}
