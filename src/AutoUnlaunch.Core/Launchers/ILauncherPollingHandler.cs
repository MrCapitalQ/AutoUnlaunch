namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public interface ILauncherPollingHandler
{
    Task InvokeAsync(CancellationToken cancellationToken);
}
