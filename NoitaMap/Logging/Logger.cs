namespace NoitaMap.Logging;

public class Logger
{
    public static ILogger CurrentLogger { get; private set; } = new ConsoleLogger();

    public static void Log(LogLevel level, string? message) => CurrentLogger.Log(level, message);

    public static void Log(LogLevel level, Exception? exception) => CurrentLogger.Log(level, exception);

    public static void LogInformation(string? message) => CurrentLogger.Log(LogLevel.Information, message);

    public static void LogInformation(Exception? exception) => CurrentLogger.Log(LogLevel.Information, exception);

    public static void LogWarning(string? message) => CurrentLogger.Log(LogLevel.Warning, message);

    public static void LogWarning(Exception? exception) => CurrentLogger.Log(LogLevel.Warning, exception);

    public static void LogCritical(string? message) => CurrentLogger.Log(LogLevel.Critical, message);

    public static void LogCritical(Exception? exception) => CurrentLogger.Log(LogLevel.Critical, exception);
}