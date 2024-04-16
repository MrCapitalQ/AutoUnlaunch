namespace MrCapitalQ.AutoUnlaunch.Core.Startup;

public enum AppStartupState
{
    Disabled,
    DisabledByUser,
    Enabled,
    DisabledByPolicy,
    EnabledByPolicy
}