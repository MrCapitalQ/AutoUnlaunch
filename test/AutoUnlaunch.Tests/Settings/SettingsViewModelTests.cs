using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Startup;
using MrCapitalQ.AutoUnlaunch.Settings;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;
using MrCapitalQ.AutoUnlaunch.Shared;
using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch.Tests.Settings;

public class SettingsViewModelTests
{
    private readonly IStartupTaskService _startupTaskService;
    private readonly ISettingsService _settingsService;
    private readonly IPackageInfo _packageInfo;
    private readonly ISteamSettingsViewModel _steamSettingsViewModel;

    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _startupTaskService = Substitute.For<IStartupTaskService>();
        _settingsService = Substitute.For<ISettingsService>();
        _packageInfo = Substitute.For<IPackageInfo>();
        _steamSettingsViewModel = Substitute.For<ISteamSettingsViewModel>();

        _viewModel = new(_startupTaskService,
            _settingsService,
            _packageInfo,
            _steamSettingsViewModel);
    }

    [Fact]
    public void Ctor_WithMatchingExitBehaviorOption_InitializesFromSettings()
    {
        var expected = AppExitBehavior.Stop;
        _settingsService.GetAppExitBehavior().Returns(expected);

        var viewModel = new SettingsViewModel(_startupTaskService,
            _settingsService,
            _packageInfo,
            _steamSettingsViewModel);

        Assert.Equal(expected, viewModel.SelectedExitBehavior.Value);
    }

    [Fact]
    public void Ctor_WithNoMatchingExitBehaviorOption_InitializesWithDefault()
    {
        var expected = AppExitBehavior.RunInBackground;
        _settingsService.GetAppExitBehavior().Returns((AppExitBehavior)100);

        var viewModel = new SettingsViewModel(_startupTaskService,
            _settingsService,
            _packageInfo,
            _steamSettingsViewModel);

        Assert.Equal(expected, viewModel.SelectedExitBehavior.Value);
    }

    [Fact]
    public void Ctor_InitializesWithPackageInfo()
    {
        var expectedAppDisplayName = "AppName";
        _packageInfo.DisplayName.Returns(expectedAppDisplayName);
        _packageInfo.Version.Returns(new PackageVersion(1, 2, 3, 0));

        var viewModel = new SettingsViewModel(_startupTaskService,
            _settingsService,
            _packageInfo,
            _steamSettingsViewModel);

        Assert.Equal(expectedAppDisplayName, viewModel.AppDisplayName);
        Assert.Equal("1.2.3", viewModel.Version);
    }

    [Fact]
    public void Ctor_SetsLauncherViewModelProperties()
    {
        Assert.Equal(_steamSettingsViewModel, _viewModel.SteamSettings);
    }

    [Fact]
    public void Ctor_AllAppExitBehaviorHasCorrespondingOption()
    {
        var expected = Enum.GetValues<AppExitBehavior>();

        var actual = _viewModel.ExitBehaviorOptions.Select(option => option.Value);

        Assert.Equivalent(expected, actual);
    }

    [InlineData(AppStartupState.Disabled, false, true, "Start automatically in the background when you sign in")]
    [InlineData(AppStartupState.DisabledByUser, false, false, "Startup is disabled at the system level and must be enabled using the Startup tab in Task Manager")]
    [InlineData(AppStartupState.Enabled, true, true, "Start automatically in the background when you sign in")]
    [InlineData(AppStartupState.DisabledByPolicy, false, false, "Startup is disabled by group policy or not supported on this device")]
    [InlineData(AppStartupState.EnabledByPolicy, true, false, "Startup is enabled by group policy")]
    [Theory]
    public void Ctor_InitializesStartupProperties(AppStartupState startupState,
        bool expectedIsStartupOn,
        bool expectedIsStartupToggleEnabled,
        string expectedStartupSettingsText)
    {
        _startupTaskService.GetStartupStateAsync().Returns(startupState);

        var viewModel = new SettingsViewModel(_startupTaskService,
            _settingsService,
            _packageInfo,
            _steamSettingsViewModel);

        Assert.Equal(expectedIsStartupOn, viewModel.IsStartupOn);
        Assert.Equal(expectedIsStartupToggleEnabled, viewModel.IsStartupToggleEnabled);
        Assert.Equal(expectedStartupSettingsText, viewModel.StartupSettingsText);
    }

    [InlineData(false, AppStartupState.Disabled, false)]
    [InlineData(true, AppStartupState.Enabled, true)]
    [Theory]
    public void IsStartupOn_SetsStartupState(bool isStartOn, AppStartupState startupState, bool expectedIsStartupOn)
    {
        _startupTaskService.GetStartupStateAsync().Returns(startupState);

        _viewModel.IsStartupOn = isStartOn;

        Assert.Equal(expectedIsStartupOn, _viewModel.IsStartupOn);
        _startupTaskService.Received(1).SetStartupStateAsync(isStartOn);
    }
}
