namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public interface ILauncherWatchingHandler
{
    string LauncherName { get; }
    bool IsEnabled { get; }
    bool IsStarted { get; }

    Task StartAsync();
    Task StopAsync();
}
