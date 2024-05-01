using Microsoft.Extensions.DependencyInjection;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Startup;
using MrCapitalQ.AutoUnlaunch.Infrastructure;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;

namespace MrCapitalQ.AutoUnlaunch.Background;

public sealed class UpdateTask : IBackgroundTask
{
    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        try
        {
            var services = new ServiceCollection()
                .AddStartupTaskService()
                .AddLocalApplicationDataStore()
                .AddSettingsService()
                .BuildServiceProvider();

            var settingsService = services.GetRequiredService<ISettingsService>();
            var startupState = await services.GetRequiredService<IStartupTaskService>().GetStartupStateAsync();
            if (startupState is AppStartupState.Enabled or AppStartupState.EnabledByPolicy
                && settingsService.GetHasBeenLaunchedOnce())
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppWithArgumentsAsync("-silent");
        }
        finally
        {
            deferral.Complete();
        }
    }
}
