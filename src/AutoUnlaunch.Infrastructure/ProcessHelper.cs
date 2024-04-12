using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessHelper
{
    private static readonly int s_currentSessionId = Process.GetCurrentProcess().SessionId;

    public static IEnumerable<Process> GetSessionProcessesByName(string processName)
        => Process.GetProcessesByName(processName).Where(x => x.SessionId == s_currentSessionId);
}
