namespace MrCapitalQ.AutoUnlaunch.Shared;

internal record class SteamStartMenuSettingsChangedMessage
{
    private SteamStartMenuSettingsChangedMessage() { }

    public static SteamStartMenuSettingsChangedMessage Instance { get; } = new();
}
