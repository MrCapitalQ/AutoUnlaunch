namespace MrCapitalQ.AutoUnlaunch.Core;

public interface IProtocolLauncher
{
    Task<bool> LaunchUriAsync(Uri uri);
}
