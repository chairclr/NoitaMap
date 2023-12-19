namespace NoitaMap.Logging;

internal class ConsoleLogger : ILogger
{
    private static object ConsoleLogLock = new object();

    public void Log(LogLevel level, string? message)
    {
        ConsoleColor color = level switch
        {
            LogLevel.Information => ConsoleColor.Gray,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Critical => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };

        string formattedMessage = $"[{DateTime.Now:HH:mm:ss}.{DateTime.Now:fff}] [{level}]{new string(' ', 11 - level.ToString().Length)}: {message}";

        lock (ConsoleLogLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(formattedMessage);
        }
    }

    public void Log(LogLevel level, Exception? exception)
    {
        ConsoleColor color = level switch
        {
            LogLevel.Information => ConsoleColor.Gray,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Critical => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };

        string formattedMessage = $"[{DateTime.Now:HH:mm:ss}.{DateTime.Now:fff}] [{level}]{new string(' ', 11 - level.ToString().Length)}: Caught exception:\n{exception}";

        lock (ConsoleLogLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(formattedMessage);
        }
    }
}