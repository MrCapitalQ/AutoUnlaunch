using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Tests.AppData;

internal class TestLauncherSettingsService : LauncherSettingsService
{
    public const string IsLauncherEnabledTestKey = "TestLauncher_IsEnabled";
    public const string LauncherStopDelayTestKey = "TestLauncher_StopDelay";
    public const string LauncherStopMethodTestKey = "TestLauncher_StopMethod";

    public TestLauncherSettingsService(IApplicationDataStore applicationDataStore) : base(applicationDataStore)
    { }

    protected override string LauncherKey => "TestLauncher";
}