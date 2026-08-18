using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.Launchers;

public class LauncherProcessTrackingHandlerTests
{
    private readonly TestProcessWatcher _processWatcher = Substitute.For<TestProcessWatcher>();
    private readonly IApplicationDataStore _applicationDataStore = Substitute.For<IApplicationDataStore>();
    private readonly TestLauncherSettingsService _launcherSettingsService;
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly FakeLogger _logger = new();
    private readonly TimeSpan _stopDelay = TimeSpan.FromSeconds(10);

    private readonly TestLauncherProcessTrackingHandler _launcherHandler;

    public LauncherProcessTrackingHandlerTests()
    {
        _launcherSettingsService = new(_applicationDataStore);

        _applicationDataStore.GetValueOrDefault("TestLauncher_StopDelay", Arg.Any<int>())
            .Returns((int)_stopDelay.TotalSeconds);

        _launcherHandler = new(_processWatcher, _launcherSettingsService, _timeProvider, _logger);
    }

    [Fact]
    public async Task StartAsync_MultipleStarts_OnlyStartsOnce()
    {
        // Act
        await _launcherHandler.StartAsync();
        await _launcherHandler.StartAsync();

        // Assert
        Assert.True(_launcherHandler.IsStarted);
        Assert.Equal("Handler for TestLauncher is already started.", _logger.LatestRecord.Message);
        _processWatcher.Received(1).GetCurrentProcesses(); // Only called as part of starting the handler.
    }

    [Fact]
    public async Task StartAsync_ActivityProcessStarted_InvokesOnLauncherActivityStarted()
    {
        // Arrange
        var activityProcess = new ProcessInfo(1, "ActivityProcess", TestLauncherProcessTrackingHandler.FakeActivityInstallPath);

        // Act
        await _launcherHandler.StartAsync();
        _processWatcher.RaiseProcessStarted(activityProcess);

        // Assert
        Assert.True(_launcherHandler.CalledOnLauncherActivityStarted);
        Assert.Contains("An activity for launcher TestLauncher started", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task StartAsync_ActivityProcessStopped_SchedulesLauncherStop()
    {
        // Arrange
        var activityProcess = new ProcessInfo(1, "ActivityProcess", TestLauncherProcessTrackingHandler.FakeActivityInstallPath);

        // Act
        await _launcherHandler.StartAsync();
        _processWatcher.RaiseProcessStarted(activityProcess);
        _processWatcher.RaiseProcessStopped(activityProcess);

        // Arrange
        Assert.Equal("An activity for launcher TestLauncher is no longer running. Stopping launcher in 10 second(s).", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task StartAsync_ActivityProcessStoppedAndStopDelayElapses_StopsLauncher()
    {
        // Arrange
        var activityProcess = new ProcessInfo(1, "ActivityProcess", TestLauncherProcessTrackingHandler.FakeActivityInstallPath);

        // Act
        await _launcherHandler.StartAsync();
        _processWatcher.RaiseProcessStarted(activityProcess);
        _processWatcher.RaiseProcessStopped(activityProcess);
        _timeProvider.Advance(_stopDelay);
        await Task.Delay(1); // Allow async operations to happen after advancing time.

        // Arrange
        Assert.Equal("Stopping launcher TestLauncher.", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task StartAsync_ActivityProcessStopped_LogsNotRunning()
    {
        var activityProcess = new ProcessInfo(1, "ActivityProcess", TestLauncherProcessTrackingHandler.FakeActivityInstallPath);

        _launcherHandler.SetIsLauncherRunning(false);

        // Act
        await _launcherHandler.StartAsync();
        _processWatcher.RaiseProcessStarted(activityProcess);
        _processWatcher.RaiseProcessStopped(activityProcess);

        // Arrange
        Assert.Equal("Launcher TestLauncher is not currently running. Launcher will not be stopped.", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task StartAsync_ActivityProcessRestartsBeforeStoppingLauncher_CancelsLauncherStop()
    {
        // Arrange
        var activityProcess = new ProcessInfo(1, "ActivityProcess", TestLauncherProcessTrackingHandler.FakeActivityInstallPath);

        // Act
        await _launcherHandler.StartAsync();
        _processWatcher.RaiseProcessStarted(activityProcess);
        _processWatcher.RaiseProcessStopped(activityProcess);
        _timeProvider.Advance(_stopDelay - TimeSpan.FromSeconds(1));
        _processWatcher.RaiseProcessStarted(activityProcess);
        await Task.Delay(1); // Allow async operations to happen after advancing time.

        // Arrange
        Assert.Equal("Scheduled stop for launcher TestLauncher was cancelled.", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task StopAsync_NotStarted_DoNothing()
    {
        // Act
        await _launcherHandler.StopAsync();

        // Assert
        Assert.False(_launcherHandler.IsStarted);
        Assert.Equal("Handler for launcher TestLauncher is already stopped.", _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task StopAsync_AfterStart_StopsHandler()
    {
        // Arrange
        await _launcherHandler.StartAsync();

        // Act
        await _launcherHandler.StopAsync();

        // Assert
        Assert.False(_launcherHandler.IsStarted);
        Assert.Equal("Stopping handler for launcher TestLauncher.", _logger.LatestRecord.Message);
    }

    private class TestLauncherProcessTrackingHandler(IProcessWatcher processWatcher,
        LauncherSettingsService launcherSettingsService,
        TimeProvider timeProvider,
        ILogger logger) : LauncherProcessTrackingHandler(processWatcher, launcherSettingsService, timeProvider, logger)
    {
        public const string FakeActivityInstallPath = "test/";

        private bool _isLauncherRunning = true;

        public bool CalledStopLauncherAsync { get; private set; }
        public bool CalledOnLauncherActivityStarted { get; private set; }
        public bool CalledOnLauncherActivityEnded { get; private set; }

        public override string LauncherName => "TestLauncher";

        public void SetIsLauncherRunning(bool isRunning) { _isLauncherRunning = isRunning; }

        protected override Task<bool> IsLauncherActivityAsync(ProcessInfo processInfo)
            => Task.FromResult(processInfo.ProcessPath?.StartsWith(FakeActivityInstallPath) == true);

        protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_isLauncherRunning);

        protected override Task StopLauncherAsync(CancellationToken cancellationToken = default)
        {
            CalledStopLauncherAsync = true;
            return Task.CompletedTask;
        }

        protected override Task OnLauncherActivityStarted(CancellationToken cancellationToken = default)
        {
            CalledOnLauncherActivityStarted = true;
            return base.OnLauncherActivityStarted(cancellationToken);
        }

        protected override Task OnLauncherActivityEnded(CancellationToken cancellationToken = default)
        {
            CalledOnLauncherActivityEnded = true;
            return base.OnLauncherActivityEnded(cancellationToken);
        }
    }

    public class TestProcessWatcher : IProcessWatcher
    {
        public event EventHandler<ProcessEventArgs>? ProcessStarted;
        public event EventHandler<ProcessEventArgs>? ProcessStopped;

        public virtual IEnumerable<ProcessInfo> GetCurrentProcesses() => [];

        public void RaiseProcessStarted(ProcessInfo processInfo) => ProcessStarted?.Invoke(this, new ProcessEventArgs(processInfo));
        public void RaiseProcessStopped(ProcessInfo processInfo) => ProcessStopped?.Invoke(this, new ProcessEventArgs(processInfo));
    }

    private class TestLauncherSettingsService(IApplicationDataStore applicationDataStore)
        : LauncherSettingsService(applicationDataStore)
    {
        protected override string LauncherKey => "TestLauncher";
        protected override LauncherStopMethod DefaultLauncherStopMethod => LauncherStopMethod.RequestShutdown;
    }
}
