using SafeEcu.Application.Common;

namespace SafeEcu.Infrastructure.Logging;

public sealed class FileAppLogger : IAppLogger
{
    private readonly string _logFilePath;
    private readonly object _syncRoot = new();

    public FileAppLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"safe-ecu-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public void Information(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        var details = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", details);
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}";

        lock (_syncRoot)
        {
            File.AppendAllText(_logFilePath, line);
        }
    }
}
