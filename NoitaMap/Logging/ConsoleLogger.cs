namespace NoitaMap.Logging;

internal class ConsoleLogger : Logger
{
    private static readonly Lock _consoleLogLock = new();

    protected override void Log(LogLevel level, string caller, string? message)
    {
        ConsoleColor color = GetColorForLogLevel(level);
        string formattedMessage = $"[{caller}/{level}]: {message}";

        using (_consoleLogLock.EnterScope())
        {
            Console.ForegroundColor = color;
            Console.WriteLine(formattedMessage);
            Console.ResetColor();
        }
    }

    protected override void Log(LogLevel level, string caller, Exception? exception)
    {
        ConsoleColor color = GetColorForLogLevel(level);
        string formattedMessage = $"[{caller}/{level}]: {exception}";

        using (_consoleLogLock.EnterScope())
        {
            Console.ForegroundColor = color;
            Console.WriteLine(formattedMessage);
            Console.ResetColor();
        }
    }

    private static ConsoleColor GetColorForLogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Info => ConsoleColor.DarkGray,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Crit => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
    }
}
