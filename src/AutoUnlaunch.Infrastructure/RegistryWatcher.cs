using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal partial class RegistryWatcher(RegistryHive hive,
    string path,
    RegistryView view,
    ILogger<RegistryWatcher> logger) : IDisposable
{
    public event EventHandler<EventArgs>? Changed;
    public event EventHandler<EventArgs>? Errored;

    private readonly RegistryHive _hive = hive;
    private readonly string _path = path;
    private readonly RegistryView _view = view;
    private readonly ILogger<RegistryWatcher> _logger = logger;
    private (string Path, RegistryKey RegistryKey)? _currentlyWatched;

    public bool IsStarted => _currentlyWatched.HasValue;

    public void Start() => StartCore();

    public void Stop() => StopCore();

    private void StartCore(bool shouldLogInitialMessage = true)
    {
        if (_currentlyWatched.HasValue)
        {
            LogWatcherAlreadyStarted(_path);
            return;
        }

        if (shouldLogInitialMessage)
            LogStartingWatcher(_path);

        TryOpenClosestRegistryKey(_path);

        if (_currentlyWatched.HasValue)
        {
            if (string.Equals(_currentlyWatched.Value.Path, _path, StringComparison.OrdinalIgnoreCase))
                LogOpenedRegistryKey(_path);
            else
                LogOpenedClosestParentToRegistryKey(_currentlyWatched.Value.Path, _path);
        }
        else
        {
            StopCore(false);
            throw new InvalidOperationException($"Unable to open registry key {_path} or one any of its parent.");
        }

        _ = Task.Run(() => RunNotifyLoop(_currentlyWatched.Value));
    }

    private void StopCore(bool shouldLogInitialMessage = true)
    {
        if (!_currentlyWatched.HasValue)
        {
            LogWatcherAlreadyStopped(_path);
            return;
        }

        if (shouldLogInitialMessage)
            LogStoppingWatcher(_path);

        _currentlyWatched.Value.RegistryKey.Dispose();
        _currentlyWatched = null;
    }

    private void Restart()
    {
        try
        {
            StopCore(false);
            StartCore(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restart registry watcher for {RegistryKeyPath}. No further events will be produced.",
                _path);
            OnErrored();
            Stop();
        }
    }

    protected void OnChanged()
    {
        var raiseEvent = Changed;
        raiseEvent?.Invoke(this, new());
    }

    protected void OnErrored()
    {
        var raiseEvent = Errored;
        raiseEvent?.Invoke(this, new());
    }

    private void TryOpenClosestRegistryKey(string path)
    {
        using var baseKey = RegistryKey.OpenBaseKey(_hive, _view);
        var key = baseKey.OpenSubKey(path);

        if (key is null)
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } parentPath)
                TryOpenClosestRegistryKey(parentPath);
            return;
        }

        _currentlyWatched = (path, key);
    }

    private void RunNotifyLoop((string Path, RegistryKey RegistryKey) target)
    {
        LogWatchingRegistryKey(target.Path);

        var filter = REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET;

        while (_currentlyWatched == target)
        {
            var notifyResult = PInvoke.RegNotifyChangeKeyValue(target.RegistryKey.Handle, true, filter, fAsynchronous: false);

            if (notifyResult is not WIN32_ERROR.ERROR_SUCCESS)
            {
                _logger.LogWarning("Error code {Win32Error} encountered while waiting for registry key change notification. Restarting watcher.",
                    notifyResult);
                Restart();
                return;
            }
            else if (!string.Equals(GetClosestPath(_hive, _path, _view), target.Path, StringComparison.OrdinalIgnoreCase))
            {
                LogRestartDueToRegistryTreeChange(_path);
                Restart();
                return;
            }
            else if (string.Equals(_path, target.Path, StringComparison.OrdinalIgnoreCase))
            {
                LogTargetRegistryTreeChanged(_path);
                OnChanged();
            }
        }

        LogNotWatchingRegistryKey(target.Path);
    }

    private static string? GetClosestPath(RegistryHive hive, string? path, RegistryView view)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var subKey = baseKey.OpenSubKey(path);

        if (subKey is null)
            return GetClosestPath(hive, Path.GetDirectoryName(path), view);

        return path;
    }

    public void Dispose() => Stop();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Registry watcher for {RegistryKeyPath} is already started.")]
    private partial void LogWatcherAlreadyStarted(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting registry watcher for {RegistryKeyPath}.")]
    private partial void LogStartingWatcher(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Opened registry key {RegistryKeyPath}.")]
    private partial void LogOpenedRegistryKey(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Opened closest parent registry key at {ParentRegistryKeyPath} for {RegistryKeyPath}.")]
    private partial void LogOpenedClosestParentToRegistryKey(string parentRegistryKeyPath, string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Registry watcher for {RegistryKeyPath} is already stopped.")]
    private partial void LogWatcherAlreadyStopped(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping registry watcher for {RegistryKeyPath}.")]
    private partial void LogStoppingWatcher(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Watching registry key {RegistryKeyPath} for changes.")]
    private partial void LogWatchingRegistryKey(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registry key {RegistryKeyPath} or one of its parent was created, deleted, or renamed. Restarting watcher.")]
    private partial void LogRestartDueToRegistryTreeChange(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registry key {RegistryKeyPath} or its sub tree changed. Raising changed event.")]
    private partial void LogTargetRegistryTreeChanged(string registryKeyPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registry key {RegistryKeyPath} is no longer being watched. Exiting notify loop.")]
    private partial void LogNotWatchingRegistryKey(string registryKeyPath);
}

internal class RegistryWatcherFactory(IServiceProvider services)
{
    private readonly IServiceProvider _services = services;

    public RegistryWatcher Create(RegistryHive hive, string name, RegistryView view = RegistryView.Default)
        => ActivatorUtilities.CreateInstance<RegistryWatcher>(_services, hive, name, view);
}
