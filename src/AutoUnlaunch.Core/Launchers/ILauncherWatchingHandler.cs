namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public interface ILauncherWatchingHandler
{
    string LauncherName { get; }
    bool IsEnabled { get; }

    Task StartAsync();
    Task StopAsync();
}
