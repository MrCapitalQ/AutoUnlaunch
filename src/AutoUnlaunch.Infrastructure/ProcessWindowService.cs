using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessWindowService(TimeProvider timeProvider, ILogger<ProcessWindowService> logger)
{
    private const int CheckDelayMilliseconds = 50;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ProcessWindowService> _logger = logger;

    /// <summary>
    /// Brute force closing of a process to ensure all of its windows are closed.
    /// </summary>
    /// <param name="processId">The ID of the process whose windows will be closed.</param>
    /// <param name="timeout">How long to try closing a process' windows before giving up.</param>
    /// <param name="continueUntilTimeout">
    /// When <see langword="true"/>, continues to close any windows that appear until timing out.
    /// When <see langword="false"/>, stops immediately when a process no longer has any windows.
    /// The default is <see langword="false"/>.
    /// </param>
    public async Task EnsureWindowsClosedAsync(int processId, TimeSpan? timeout = null, bool continueUntilTimeout = false)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(1);

        if (continueUntilTimeout)
            _logger.LogInformation("Closing all windows for process {ProcessId} until timing out after {Timeout} ms.",
                processId,
                actualTimeout.TotalMilliseconds);
        else
            _logger.LogInformation("Closing all windows for process {ProcessId} until no open windows or timing out after {Timeout} ms.",
                processId,
                actualTimeout.TotalMilliseconds);

        var endTime = _timeProvider.GetUtcNow().Add(actualTimeout);
        nint? previouslyClosedWindow = null;

        while (_timeProvider.GetUtcNow() < endTime)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                if (process.MainWindowHandle == 0)
                {
                    if (!continueUntilTimeout)
                    {
                        _logger.LogInformation("Process {ProcessId} has no open windows.", processId);
                        return;
                    }

                    _logger.LogDebug("Process {ProcessId} has no open windows. Checking again in {Delay} ms.",
                        process.Id,
                        CheckDelayMilliseconds);
                    await Task.Delay(CheckDelayMilliseconds);
                    continue;
                }

                if (previouslyClosedWindow == process.MainWindowHandle)
                {
                    _logger.LogDebug("Process {ProcessId} main window {WindowHandle} that was previously requested to be closed is still open. Checking again in {Delay} ms.",
                        process.Id,
                        process.MainWindowHandle,
                        CheckDelayMilliseconds);
                    await Task.Delay(CheckDelayMilliseconds);
                    continue;
                }

                _logger.LogInformation("Closing current main window with title '{WindowTitle}' ({WindowHandle}) for process {ProcessName} ({ProcessId}).",
                    process.MainWindowTitle,
                    process.MainWindowHandle,
                    process.ProcessName,
                    process.Id);

                process.CloseMainWindow();
                previouslyClosedWindow = process.MainWindowHandle;
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Process {ProcessId} is not running.", processId);
                break;
            }
        }
    }
}
