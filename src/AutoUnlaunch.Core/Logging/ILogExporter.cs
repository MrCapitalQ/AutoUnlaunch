namespace MrCapitalQ.AutoUnlaunch.Core.Logging;

public interface ILogExporter
{
    Task ExportLogsAsync();
}
