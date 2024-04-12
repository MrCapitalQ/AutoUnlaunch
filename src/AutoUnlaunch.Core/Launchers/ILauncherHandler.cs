namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public interface ILauncherHandler
{
    Task InvokeAsync(CancellationToken cancellationToken);
}
