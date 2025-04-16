using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NoitaMap.Logging;

public static class Log
{
    private static Logger CurrentLogger { get; set; } = new ConsoleLogger();

    public static void LogInfo(string? message, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogInfo(message, caller);
    }

    public static void LogInfo(Exception? exception, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogInfo(exception, caller);
    }

    public static void LogWarn(string? message, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogWarn(message, caller);
    }

    public static void LogWarn(Exception? exception, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogWarn(exception, caller);
    }

    public static void LogCrit(string? message, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogCrit(message, caller);
    }

    public static void LogCrit(string? message, StackTrace stackTrace, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogCrit(message, stackTrace, caller);
    }

    public static void LogCrit(Exception? exception, [CallerMemberName] string caller = "")
    {
        CurrentLogger.LogCrit(exception, caller);
    }
}
