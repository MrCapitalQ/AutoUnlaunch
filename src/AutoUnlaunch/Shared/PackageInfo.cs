using System.Diagnostics.CodeAnalysis;
using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch.Shared;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresPackageContext)]
internal class PackageInfo : IPackageInfo
{
    public string DisplayName => Package.Current.DisplayName;

    public PackageVersion Version => Package.Current.Id.Version;
}
