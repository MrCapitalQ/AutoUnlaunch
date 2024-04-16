using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch.Shared;

public interface IPackageInfo
{
    string DisplayName { get; }
    PackageVersion Version { get; }
}
