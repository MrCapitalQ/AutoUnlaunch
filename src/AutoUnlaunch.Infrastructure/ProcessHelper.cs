using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessHelper
{
    private static readonly int s_currentSessionId = Process.GetCurrentProcess().SessionId;

    public static ProcessCollectionResult GetSessionProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        var result = processes.Where(x => x.SessionId == s_currentSessionId);
        return new ProcessCollectionResult(processes, result);
    }

    public static ProcessCollectionResult GetSessionProcessesByParent(int parentProcessId, IEnumerable<string>? excludedProcessNames = null)
    {
        var processes = Process.GetProcesses();
        var result = processes
            .Where(x => x.SessionId == s_currentSessionId
                && x.GetParentProcessId() == parentProcessId
                && (excludedProcessNames is null || !excludedProcessNames.Contains(x.ProcessName, StringComparer.OrdinalIgnoreCase)));
        return new ProcessCollectionResult(processes, result);
    }
}
