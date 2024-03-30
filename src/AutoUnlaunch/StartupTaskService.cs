using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch;

internal class StartupTaskService
{
    private const string StartupTaskId = "b289bd30-d771-44a6-ac8c-1e7c0a5b3d17";

    public async Task<StartupTaskState> GetStartupStateAsync()
    {
        var startupTask = await StartupTask.GetAsync(StartupTaskId);
        return startupTask.State;
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
