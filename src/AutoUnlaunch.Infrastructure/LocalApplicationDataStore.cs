using MrCapitalQ.AutoUnlaunch.Core.AppData;
using Windows.Storage;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

public class LocalApplicationDataStore : IApplicationDataStore
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public object? GetValue(string key) => _localSettings.Values[key];
    public void SetValue(string key, object? value) => _localSettings.Values[key] = value;
}
