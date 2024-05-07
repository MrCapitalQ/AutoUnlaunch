using MrCapitalQ.AutoUnlaunch.Core;
using Windows.System;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProtocolLauncher : IProtocolLauncher
{
    public async Task<bool> LaunchUriAsync(Uri uri) => await Launcher.LaunchUriAsync(uri);
}
