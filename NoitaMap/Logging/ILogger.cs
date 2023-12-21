using System.Diagnostics;

namespace NoitaMap.Logging;

public interface ILogger
{
    public void Log(LogLevel level, string? message);

    public void Log(LogLevel level, Exception? exception);

    public void LogInformation(string? message) => Log(LogLevel.Information, message);

    public void LogInformation(Exception? exception) => Log(LogLevel.Information, exception);

    public void LogWarning(string? message) => Log(LogLevel.Warning, message);

    public void LogWarning(Exception? exception) => Log(LogLevel.Warning, exception);

    public void LogCritical(string? message) => Log(LogLevel.Critical, message);

    public void LogCritical(string? message, StackTrace stackTrace) 
    {
        Log(LogLevel.Critical, message);
        Log(LogLevel.Critical, $"Stack trace:\n{stackTrace}");
    }

    public void LogCritical(Exception? exception) => Log(LogLevel.Critical, exception);
}