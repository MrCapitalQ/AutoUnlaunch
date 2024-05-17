using Microsoft.Extensions.Caching.Memory;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using Windows.Storage;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

public class LocalApplicationDataStore(IMemoryCache cache) : IApplicationDataStore
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
    private readonly IMemoryCache _cache = cache;

    public object? GetValue(string key)
    {
        if (_cache.TryGetValue(key, out var value))
            return value;

        return _cache.Set(key, _localSettings.Values[key]);
    }

    public void SetValue(string key, object? value)
    {
        _cache.Set(key, value);
        _localSettings.Values[key] = value;
    }
}
