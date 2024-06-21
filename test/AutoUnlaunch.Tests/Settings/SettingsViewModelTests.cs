using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.UI.Xaml.Media.Animation;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Startup;
using MrCapitalQ.AutoUnlaunch.Settings;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.EA;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Epic;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Gog;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;
using MrCapitalQ.AutoUnlaunch.Shared;
using NSubstitute.ExceptionExtensions;
using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch.Tests.Settings;

public class SettingsViewModelTests
{
    private readonly IStartupTaskService _startupTaskService;
    private readonly ISettingsService _settingsService;
    private readonly IPackageInfo _packageInfo;
    private readonly IMessenger _messenger;
    private readonly ISteamSettingsViewModel _steamSettingsViewModel;
    private readonly IEASettingsViewModel _eaSettingsViewModel;
    private readonly IGogSettingsViewModel _gogSettingsViewModel;
    private readonly IEpicSettingsViewModel _epicSettingsViewModel;
    private readonly FakeLogger<SettingsViewModel> _logger;

    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _startupTaskService = Substitute.For<IStartupTaskService>();
        _settingsService = Substitute.For<ISettingsService>();
        _packageInfo = Substitute.For<IPackageInfo>();
        _messenger = Substitute.For<IMessenger>();
        _steamSettingsViewModel = Substitute.For<ISteamSettingsViewModel>();
        _eaSettingsViewModel = Substitute.For<IEASettingsViewModel>();
        _gogSettingsViewModel = Substitute.For<IGogSettingsViewModel>();
        _epicSettingsViewModel = Substitute.For<IEpicSettingsViewModel>();
        _logger = new FakeLogger<SettingsViewModel>();

        _viewModel = new(_startupTaskService,
            _settingsService,
            _packageInfo,
            _messenger,
            _steamSettingsViewModel,
            _eaSettingsViewModel,
            _gogSettingsViewModel,
            _epicSettingsViewModel,
            _logger);
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
            _messenger,
            _steamSettingsViewModel,
            _eaSettingsViewModel,
            _gogSettingsViewModel,
            _epicSettingsViewModel,
            _logger);

        Assert.Equal(expectedAppDisplayName, viewModel.AppDisplayName);
        Assert.Equal("1.2.3", viewModel.Version);
    }

    [Fact]
    public void Ctor_SetsLauncherViewModelProperties()
    {
        Assert.Equal(_steamSettingsViewModel, _viewModel.SteamSettings);
        Assert.Equal(_eaSettingsViewModel, _viewModel.EASettings);
        Assert.Equal(_gogSettingsViewModel, _viewModel.GogSettings);
        Assert.Equal(_epicSettingsViewModel, _viewModel.EpicSettings);
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
            _messenger,
            _steamSettingsViewModel,
            _eaSettingsViewModel,
            _gogSettingsViewModel,
            _epicSettingsViewModel,
            _logger);

        Assert.Equal(expectedIsStartupOn, viewModel.IsStartupOn);
        Assert.Equal(expectedIsStartupToggleEnabled, viewModel.IsStartupToggleEnabled);
        Assert.Equal(expectedStartupSettingsText, viewModel.StartupSettingsText);
    }

    [InlineData(false, AppStartupState.Disabled, false)]
    [InlineData(true, AppStartupState.Enabled, true)]
    [Theory]
    public void SetIsStartupOn_SetsStartupState(bool isStartOn, AppStartupState startupState, bool expectedIsStartupOn)
    {
        _startupTaskService.GetStartupStateAsync().Returns(startupState);

        _viewModel.IsStartupOn = isStartOn;

        Assert.Equal(expectedIsStartupOn, _viewModel.IsStartupOn);
        _startupTaskService.Received(1).SetStartupStateAsync(isStartOn);
    }

    [Fact]
    public void SetIsStartupOn_ExceptionThrown_LogsError()
    {
        var expectedException = new Exception("Test exception.");
        _startupTaskService.GetStartupStateAsync().ThrowsAsync(expectedException);

        _viewModel.IsStartupOn = true;

        Assert.Equal("An error occurred while updating application startup state.", _logger.LatestRecord.Message);
        Assert.Equal(expectedException, _logger.LatestRecord.Exception);
    }

    [Fact]
    public void AdvancedSettingsCommand_SendsNavigateMessage()
    {
        var navigateMessage = new SlideNavigateMessage(typeof(AdvancedSettingsPage), SlideNavigationTransitionEffect.FromRight);

        _viewModel.AdvancedSettingsCommand.Execute(null);

        _messenger.Received(1).Send<NavigateMessage, TestMessengerToken>(navigateMessage, Arg.Any<TestMessengerToken>());
    }
}
