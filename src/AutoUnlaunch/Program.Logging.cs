using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Diagnostics.CodeAnalysis;
using Windows.Storage;

namespace MrCapitalQ.AutoUnlaunch;

[ExcludeFromCodeCoverage]
public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder loggingBuilder)
    {
        var loggingLevelSwitch = new LoggingLevelSwitch();
        loggingBuilder.Services.TryAddSingleton<ILogLevelManager>(s => ActivatorUtilities.CreateInstance<LogLevelManager>(s, loggingLevelSwitch));

        var logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .WriteTo.File(new CompactJsonFormatter(),
                Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Logs\log.txt"),
                fileSizeLimitBytes: 1024 * 1024 * 10, // 10 MB
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
        return loggingBuilder.AddSerilog(logger, dispose: true);
    }

    public static void UseLogLevelSettings(this IHost host)
    {
        var settingsService = host.Services.GetRequiredService<ISettingsService>();
        host.Services.GetRequiredService<ILogLevelManager>().SetMinimumLogLevel(settingsService.GetMinimumLogLevel());
    }

    private class LogLevelManager(LoggingLevelSwitch loggingLevelSwitch, IConfiguration configuration) : ILogLevelManager
    {
        private readonly LoggingLevelSwitch _loggingLevelSwitch = loggingLevelSwitch;
        private readonly IConfiguration _configuration = configuration;

        public void SetMinimumLogLevel(LogLevel logLevel)
        {
            _configuration["Logging:LogLevel:Default"] = logLevel.ToString();
            (_configuration as IConfigurationRoot)?.Reload();

            _loggingLevelSwitch.MinimumLevel = logLevel switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                _ => (LogEventLevel)int.MaxValue,
            };
        }
    }
}
