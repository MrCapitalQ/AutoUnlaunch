using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.Launchers;

public class LauncherHandlerTests
{
    private readonly IApplicationDataStore _applicationDataStore;
    private readonly TestLauncherSettingsService _launcherSettingsService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly FakeLogger _logger;

    private readonly TestLauncherHandler _launcherHandler;

    public LauncherHandlerTests()
    {
        _applicationDataStore = Substitute.For<IApplicationDataStore>();
        _launcherSettingsService = new(_applicationDataStore);
        _timeProvider = new();
        _logger = new();

        _launcherHandler = new(_launcherSettingsService, _timeProvider, _logger);
    }

    [Fact]
    public async Task InvokeAsync_LauncherNotEnabled_DoesNothing()
    {
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.Equal("Launcher handler for TestLauncher is disabled.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, _logger.LatestRecord.Level);
        _applicationDataStore.Received(1).GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, true);
    }

    [Fact]
    public async Task InvokeAsync_LauncherNotRunning_DoesNothing()
    {
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.True(_launcherHandler.CalledIsLauncherRunningAsync);
        Assert.Equal("TestLauncher is currently not running.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task InvokeAsync_LauncherActivityRunning_DetectsActivity()
    {
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.True(_launcherHandler.CalledIsLauncherActivityRunningAsync);
        Assert.Equal("An activity for TestLauncher is currently running.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task InvokeAsync_LauncherRunningWithNoActivity_SchedulesDelayedStopTime()
    {
        #region Arrange
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.LauncherStopDelayTestKey, Arg.Any<int>()).Returns(10);

        // Sets up condition where a launcher and an launcher activity is running so the handler knows it should
        // schedule a stop time when the activity is no longer running.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where a launcher activity is no longer running.
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = false;
        #endregion

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.Equal("An activity for TestLauncher is no longer running. Stopping launcher in 10 second(s).", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task InvokeAsync_LauncherNotRunningWithScheduledStopTime_CancelsStop()
    {
        #region Arrange
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.LauncherStopDelayTestKey, Arg.Any<int>()).Returns(10);

        // Sets up condition where a launcher and an launcher activity is running so the handler knows it should
        // schedule a stop time when the activity is no longer running.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where a launcher activity is no longer running, scheduling a stop time.
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = false;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where the launcher is no longer running.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = false;
        #endregion

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.Equal("TestLauncher is not currently running. Cancelling scheduled stop.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task InvokeAsync_LauncherActivityRunningWithScheduledStopTime_CancelsStop()
    {
        #region Arrange
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.LauncherStopDelayTestKey, Arg.Any<int>()).Returns(10);

        // Sets up condition where a launcher and an launcher activity is running so the handler knows it should
        // schedule a stop time when the activity is no longer running.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where a launcher activity is no longer running, scheduling a stop time.
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = false;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where the launcher is running again.
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;
        #endregion

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.Equal("An activity for TestLauncher is currently running. Cancelling scheduled stop.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task InvokeAsync_OnScheduledStopTime_StopLauncher()
    {
        #region Arrange
        var delay = 10;
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.LauncherStopDelayTestKey, Arg.Any<int>()).Returns(delay);

        // Sets up condition where a launcher and an launcher activity is running so the handler knows it should
        // schedule a stop time when the activity is no longer running.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where a launcher activity is no longer running, scheduling a stop time.
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = false;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where time has advanced to the scheduled stop time.
        _timeProvider.Advance(TimeSpan.FromSeconds(delay));
        #endregion

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.True(_launcherHandler.CalledStopLauncherAsync);
        Assert.Equal("Stopping TestLauncher.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
    }

    [InlineData(0)]
    [InlineData(1)]
    [Theory]
    public async Task InvokeAsync_AfterScheduledStopTime_StopLauncher(int secondsAfterScheduledStop)
    {
        #region Arrange
        var delay = 10;
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.LauncherStopDelayTestKey, Arg.Any<int>()).Returns(delay);

        // Sets up condition where a launcher and an launcher activity is running so the handler knows it should
        // schedule a stop time when the activity is no longer running.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = true;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where a launcher activity is no longer running, scheduling a stop time.
        _launcherHandler.IsLauncherActivityRunningAsyncReturnValue = false;
        await _launcherHandler.InvokeAsync(CancellationToken.None);

        // Sets up the condition where time has advanced to the scheduled stop time.
        _timeProvider.Advance(TimeSpan.FromSeconds(delay + secondsAfterScheduledStop));
        #endregion

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.True(_launcherHandler.CalledStopLauncherAsync);
        Assert.Equal("Stopping TestLauncher.", _logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
    }

    [Fact]
    public async Task InvokeAsync_LauncherRunningButNoActivityStarted_DoesNotTryToStopLauncher()
    {
        #region Arrange
        _applicationDataStore.GetValueOrDefault(TestLauncherSettingsService.IsLauncherEnabledTestKey, Arg.Any<bool>()).Returns(true);

        // Sets up condition where a launcher is running so the handler but no launcher activity has been detected yet.
        _launcherHandler.IsLauncherRunningAsyncReturnValue = true;
        await _launcherHandler.InvokeAsync(CancellationToken.None);
        #endregion

        await _launcherHandler.InvokeAsync(CancellationToken.None);

        Assert.False(_launcherHandler.CalledStopLauncherAsync);
    }

    private class TestLauncherHandler : LauncherHandler
    {
        public TestLauncherHandler(LauncherSettingsService launcherSettingsService,
            TimeProvider timeProvider,
            ILogger logger)
            : base(launcherSettingsService, timeProvider, logger)
        { }

        public bool CalledIsLauncherActivityRunningAsync { get; private set; }
        public bool CalledIsLauncherRunningAsync { get; private set; }
        public bool CalledStopLauncherAsync { get; private set; }
        public bool IsLauncherActivityRunningAsyncReturnValue { get; set; }
        public bool IsLauncherRunningAsyncReturnValue { get; set; }
        protected override string LauncherName => "TestLauncher";

        protected override Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken)
        {
            CalledIsLauncherActivityRunningAsync = true;
            return Task.FromResult(IsLauncherActivityRunningAsyncReturnValue);
        }

        protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
        {
            CalledIsLauncherRunningAsync = true;
            return Task.FromResult(IsLauncherRunningAsyncReturnValue);
        }

        protected override Task StopLauncherAsync(CancellationToken cancellationToken)
        {
            CalledStopLauncherAsync = true;
            return Task.CompletedTask;
        }
    }

    private class TestLauncherSettingsService(IApplicationDataStore applicationDataStore)
        : LauncherSettingsService(applicationDataStore)
    {
        public const string IsLauncherEnabledTestKey = "TestLauncher_IsEnabled";
        public const string LauncherStopDelayTestKey = "TestLauncher_StopDelay";
        public const string LauncherStopMethodTestKey = "TestLauncher_StopMethod";

        protected override string LauncherKey => "TestLauncher";
        protected override LauncherStopMethod DefaultLauncherStopMethod => LauncherStopMethod.RequestShutdown;
    }
}
