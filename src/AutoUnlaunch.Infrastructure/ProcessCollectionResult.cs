using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessCollectionResult(Process[] original, IEnumerable<Process> items) : IDisposable
{
    private readonly Process[] _original = original;

    public IEnumerable<Process> Items { get; } = items;

    public void Dispose()
    {
        foreach (var process in _original)
            process.Dispose();
    }
}
