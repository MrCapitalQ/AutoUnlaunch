using MrCapitalQ.AutoUnlaunch.Core.Startup;
using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class StartupTaskService : IStartupTaskService
{
    private const string StartupTaskId = "b289bd30-d771-44a6-ac8c-1e7c0a5b3d17";

    public async Task<AppStartupState> GetStartupStateAsync()
    {
        var startupTask = await StartupTask.GetAsync(StartupTaskId);
        return (AppStartupState)startupTask.State;
    }

    public async Task SetStartupStateAsync(bool isEnabled)
    {
        var startupTask = await StartupTask.GetAsync(StartupTaskId);
        if (isEnabled)
            await startupTask.RequestEnableAsync();
        else
            startupTask.Disable();
    }
}
