using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessWindowService(TimeProvider timeProvider, ILogger<ProcessWindowService> logger)
{
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
        var endTime = _timeProvider.GetUtcNow().Add(timeout ?? TimeSpan.FromSeconds(1));
        nint? previouslyClosedWindow = null;

        while (_timeProvider.GetUtcNow() < endTime)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                if (process.MainWindowHandle == 0)
                {
                    if (!continueUntilTimeout)
                        break;

                    await Task.Delay(50);
                    continue;
                }

                if (previouslyClosedWindow == process.MainWindowHandle)
                {
                    await Task.Delay(50);
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
            catch
            {
                break;
            }
        }
    }
}
