namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public interface ILauncherTrackingHandler
{
    string LauncherName { get; }
    bool IsEnabled { get; }
    bool IsStarted { get; }

    Task StartAsync();
    Task StopAsync();
}
