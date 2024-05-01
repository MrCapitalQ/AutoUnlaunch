namespace MrCapitalQ.AutoUnlaunch.Core.Startup;

public interface IStartupTaskService
{
    Task<AppStartupState> GetStartupStateAsync();
    Task SetStartupStateAsync(bool isEnabled);
}
