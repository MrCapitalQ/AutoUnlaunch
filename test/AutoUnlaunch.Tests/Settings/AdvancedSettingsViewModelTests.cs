using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Logging;
using MrCapitalQ.AutoUnlaunch.Settings;
using MrCapitalQ.AutoUnlaunch.Shared;
using NSubstitute.ExceptionExtensions;

namespace MrCapitalQ.AutoUnlaunch.Tests.Settings;

public class AdvancedSettingsViewModelTests
{
    private readonly ISettingsService _settingsService;
    private readonly ILogLevelManager _logLevelManager;
    private readonly ILogExporter _logExporter;
    private readonly IMessenger _messenger;
    private readonly FakeLogger<AdvancedSettingsViewModel> _logger;
    private readonly FakeTimeProvider _timeProvider;

    private readonly AdvancedSettingsViewModel _viewModel;

    public AdvancedSettingsViewModelTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _logLevelManager = Substitute.For<ILogLevelManager>();
        _logExporter = Substitute.For<ILogExporter>();
        _messenger = Substitute.For<IMessenger>();
        _logger = new FakeLogger<AdvancedSettingsViewModel>();
        _timeProvider = new FakeTimeProvider();

        _viewModel = new(_settingsService,
            _logLevelManager,
            _logExporter,
            _messenger,
            _logger);
    }

    [Fact]
    public void Ctor_WithMatchingExitBehaviorOption_InitializesFromSettings()
    {
        var expected = AppExitBehavior.Stop;
        _settingsService.GetAppExitBehavior().Returns(expected);

        var viewModel = new AdvancedSettingsViewModel(_settingsService,
            _logLevelManager,
            _logExporter,
            _messenger,
            _logger);

        Assert.Equal(expected, viewModel.SelectedExitBehavior.Value);
    }

    [Fact]
    public void Ctor_WithNoMatchingExitBehaviorOption_InitializesWithDefault()
    {
        var expected = AppExitBehavior.RunInBackground;
        _settingsService.GetAppExitBehavior().Returns((AppExitBehavior)100);

        var viewModel = new AdvancedSettingsViewModel(_settingsService,
            _logLevelManager,
            _logExporter,
            _messenger,
            _logger);

        Assert.Equal(expected, viewModel.SelectedExitBehavior.Value);
    }

    [Fact]
    public void Ctor_WithMatchingLogLevelOption_InitializesFromSettings()
    {
        var expected = LogLevel.Debug;
        _settingsService.GetMinimumLogLevel().Returns(expected);

        var viewModel = new AdvancedSettingsViewModel(_settingsService,
            _logLevelManager,
            _logExporter,
            _messenger,
            _logger);

        Assert.Equal(expected, viewModel.SelectedLogLevel.Value);
    }

    [Fact]
    public void Ctor_WithNoMatchingLogLevelOption_InitializesWithDefault()
    {
        var expected = LogLevel.Information;
        _settingsService.GetMinimumLogLevel().Returns((LogLevel)100);

        var viewModel = new AdvancedSettingsViewModel(_settingsService,
            _logLevelManager,
            _logExporter,
            _messenger,
            _logger);

        Assert.Equal(expected, viewModel.SelectedLogLevel.Value);
    }

    [Fact]
    public void Ctor_AllAppExitBehaviorHasCorrespondingOption()
    {
        var expected = Enum.GetValues<AppExitBehavior>();

        var actual = _viewModel.ExitBehaviorOptions.Select(option => option.Value);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void SetSelectedExitBehavior_UpdatesSettingsService()
    {
        var value = AppExitBehavior.Stop;

        _viewModel.SelectedExitBehavior = new(value, string.Empty);

        _settingsService.Received(1).SetAppExitBehavior(value);
    }

    [Fact]
    public void SetSelectedLoggingLevel_UpdatesSettingsServiceAndShowsWarningForDebug()
    {
        var value = LogLevel.Debug;

        _viewModel.SelectedLogLevel = new(value, string.Empty);

        Assert.True(_viewModel.IsLoggingLevelWarningVisible);
        _settingsService.Received(1).SetMinimumLogLevel(value);
        _logLevelManager.Received(1).SetMinimumLogLevel(value);
    }

    [Fact]
    public async Task ExportLogsCommand_CallsLogExporterAndSetsIsExportingWhileBusy()
    {
        _logExporter.ExportLogsAsync().Returns(Task.Delay(TimeSpan.FromSeconds(1), _timeProvider));

        _ = _viewModel.ExportLogsCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsExporting);
        await _logExporter.Received(1).ExportLogsAsync();

        _timeProvider.Advance(TimeSpan.FromSeconds(1));

        Assert.False(_viewModel.IsExporting);
    }

    [Fact]
    public async Task ExportLogsCommand_ExceptionThrown_LogsErrorAndShowsDialogMessage()
    {
        var expectedException = new Exception("Test exception");
        _logExporter.ExportLogsAsync().ThrowsAsync(expectedException);
        var message = new ShowDialogMessage("Error", "Something went wrong while exporting the logs.");

        await _viewModel.ExportLogsCommand.ExecuteAsync(null);

        Assert.Equal("An error occurred while exporting the application logs.", _logger.LatestRecord.Message);
        Assert.Equal(expectedException, _logger.LatestRecord.Exception);
        _messenger.Received(1).Send(message, Arg.Any<TestMessengerToken>());
    }
}
