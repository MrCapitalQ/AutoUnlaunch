namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public interface ILauncherHandler
{
    string LauncherName { get; }
    bool IsEnabled { get; }
    bool IsStarted { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
