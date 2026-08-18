using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class RegistryWatcher(string name, RegistryView view = RegistryView.Default)
{
    public event EventHandler<EventArgs>? Changed;

    private readonly string _name = name;
    private readonly RegistryView _view = view;

    private HKEY? _key;

    public bool IsStarted => _key.HasValue;

    public void Start()
    {
        if (_key.HasValue)
            return;

        var flags = _view switch
        {
            RegistryView.Registry64 => REG_SAM_FLAGS.KEY_READ | REG_SAM_FLAGS.KEY_WOW64_64KEY,
            RegistryView.Registry32 => REG_SAM_FLAGS.KEY_READ | REG_SAM_FLAGS.KEY_WOW64_32KEY,
            _ => REG_SAM_FLAGS.KEY_READ
        };

        HKEY key;
        unsafe
        {
            fixed (char* keyNamePointer = _name)
            {
                var result = PInvoke.RegOpenKeyEx(HKEY.HKEY_LOCAL_MACHINE, keyNamePointer, 0, flags, &key);
                // TODO: If error is ERROR_FILE_NOT_FOUND, do we set up a different watch to check for when this appears?
                if (result is not WIN32_ERROR.ERROR_SUCCESS)
                {
                    // TODO: Log
                    // TODO: throw?
                    return;
                }

                _key = key;
            }
        }

        _ = Task.Run(RunNotifyLoop);
    }

    public void Stop()
    {
        if (!_key.HasValue)
            return;

        PInvoke.RegCloseKey(_key.Value);
        _key = null;
    }

    protected void OnChanged()
    {
        var raiseEvent = Changed;
        raiseEvent?.Invoke(this, new());
    }

    private void RunNotifyLoop()
    {
        var filter = REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET;

        while (_key.HasValue)
        {
            var notifyResult = PInvoke.RegNotifyChangeKeyValue(_key.Value, true, filter, HANDLE.Null, false);
            // TODO: If error is ERROR_KEY_DELETED, do we set up a different watch to check for when this reappears?
            if (notifyResult is not WIN32_ERROR.ERROR_SUCCESS)
            {
                // TODO: Log
                // TODO: Emit error
                Stop();
                continue;
            }

            // TODO: Check for name change of the target key before emitting

            OnChanged();
        }
    }
}

internal class RegistryWatcherFactory(IServiceProvider services)
{
    private readonly IServiceProvider _services = services;

    public RegistryWatcher Create(string name, RegistryView view = RegistryView.Default)
        => ActivatorUtilities.CreateInstance<RegistryWatcher>(_services, name, view);
}
