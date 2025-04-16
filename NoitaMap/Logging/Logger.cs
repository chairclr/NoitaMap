using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NoitaMap.Logging;

public abstract class Logger
{
    protected abstract void Log(LogLevel level, string caller, string? message);
    protected abstract void Log(LogLevel level, string caller, Exception? exception);

    public void LogInfo(string? message, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Info, caller, message);
    }

    public void LogInfo(Exception? exception, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Info, caller, exception);
    }

    public void LogWarn(string? message, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Warn, caller, message);
    }

    public void LogWarn(Exception? exception, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Warn, caller, exception);
    }

    public void LogCrit(string? message, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Crit, caller, message);
    }

    public void LogCrit(string? message, StackTrace stackTrace, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Crit, caller, message);
        Log(LogLevel.Crit, caller, $"Stack trace:\n{stackTrace}");
    }

    public void LogCrit(Exception? exception, [CallerMemberName] string caller = "")
    {
        Log(LogLevel.Crit, caller, exception);
    }
}