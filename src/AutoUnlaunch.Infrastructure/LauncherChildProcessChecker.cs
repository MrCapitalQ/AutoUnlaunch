using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class LauncherChildProcessChecker
{
    private readonly ILogger<LauncherChildProcessChecker> _logger;

    private int? _currentActivityProcessId;

    public LauncherChildProcessChecker(ILogger<LauncherChildProcessChecker> logger) => _logger = logger;

    public bool IsChildProcessRunning(string launcherProcessName, IEnumerable<string> excludedProcessNames)
    {
        // If a launcher activity process was previously found, check to see if it's still running.
        if (_currentActivityProcessId is not null)
        {
            // Check to see if cached activity process ID by getting the Process object. An ArgumentException
            // will be thrown if it's not running anymore so clear the cached process ID.
            try
            {
                _logger.LogTrace("Checking to see if cached child process {ProcessId} of {ProcessName} is still running.",
                    _currentActivityProcessId,
                    launcherProcessName);

                Process.GetProcessById(_currentActivityProcessId.Value);

                _logger.LogTrace("Cached child process {ProcessId} of {ProcessName} is still running.",
                    _currentActivityProcessId,
                    launcherProcessName);

                return true;
            }
            catch (ArgumentException)
            {
                _logger.LogTrace("Cached child process {ProcessId} of {ProcessName} is no longer running.",
                    _currentActivityProcessId,
                    launcherProcessName);
                _currentActivityProcessId = null;
            }
        }

        _logger.LogTrace("Checking for a child process of {ProcessName}.", launcherProcessName);

        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(launcherProcessName);
        var launcherProcess = launcherProcessesResult.Items.FirstOrDefault();
        if (launcherProcess is null)
            return false;

        // Look for a child process of the launcher to see if it has launched any activity. Cache that process if one
        // is found to avoid repeated this check unnecessarily.
        using var childProcessesResult = ProcessHelper.GetSessionProcessesByParent(launcherProcess.Id, excludedProcessNames);
        _currentActivityProcessId = childProcessesResult.Items
            .FirstOrDefault()
            ?.Id;

        return _currentActivityProcessId is not null;
    }
}
