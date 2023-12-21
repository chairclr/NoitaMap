using System.Diagnostics;

namespace NoitaMap.Logging;

public class Logger
{
    public static ILogger CurrentLogger { get; private set; } = new ConsoleLogger();

    public static void Log(LogLevel level, string? message) => CurrentLogger.Log(level, message);

    public static void Log(LogLevel level, Exception? exception) => CurrentLogger.Log(level, exception);

    public static void LogInformation(string? message) => CurrentLogger.LogInformation(message);

    public static void LogInformation(Exception? exception) => CurrentLogger.LogInformation(exception);

    public static void LogWarning(string? message) => CurrentLogger.LogWarning(message);

    public static void LogWarning(Exception? exception) => CurrentLogger.LogWarning(exception);

    public static void LogCritical(string? message) => CurrentLogger.LogCritical(message);

    public static void LogCritical(string? message, StackTrace stackTrace) => CurrentLogger.LogCritical(message, stackTrace);

    public static void LogCritical(Exception? exception) => CurrentLogger.LogCritical(exception);
}