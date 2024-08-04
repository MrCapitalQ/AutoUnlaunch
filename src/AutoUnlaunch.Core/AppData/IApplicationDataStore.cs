namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public interface IApplicationDataStore
{
    object? GetValue(string key);
    T GetValueOrDefault<T>(string key, T defaultValue);
    void SetValue(string key, object? value);
}
