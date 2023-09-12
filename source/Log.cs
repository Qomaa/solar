using System.Runtime.CompilerServices;

/// <summary>
/// Class for simple logging.
/// </summary>
internal static class Log
{
    public enum Level
    {
        T = -1,
        I = 0,
        W = 1,
        E = 2,
        N = 999,
    }

    private static bool LogConsole = true;

    static Log()
    {
        if (string.IsNullOrWhiteSpace(Program.InputArgs.LogDirectory))
        {
            return;
        }

        try
        {
            System.IO.Directory.CreateDirectory(Program.InputArgs.LogDirectory);
            LogConsole = false;
        }
        catch (System.Exception)
        {
            // Es wird in Console geschrieben.
        }
    }

    public static void Exception(Exception ex, string message = "", [CallerMemberName] string callerMemberName = "")
    {
        string mes =
            (string.IsNullOrWhiteSpace(message) ? "" : message + Environment.NewLine) +
            ex.GetType() + Environment.NewLine +
            ex.Message + Environment.NewLine +
            ex.StackTrace;

        Error(mes, callerMemberName);
    }

    public static void Trace(string message, [CallerMemberName] string callerMemberName = "")
    {
        WriteMessage(message, Level.T, callerMemberName);
    }

    public static void Info(string message, [CallerMemberName] string callerMemberName = "")
    {
        WriteMessage(message, Level.I, callerMemberName);
    }

    public static void Error(string message, [CallerMemberName] string callerMemberName = "")
    {
        WriteMessage(message, Level.E, callerMemberName);
    }

    public static void Warning(string message, [CallerMemberName] string callerMemberName = "")
    {
        WriteMessage(message, Level.W, callerMemberName);
    }

    private static void WriteMessage(string message, Level level, string callerMethod)
    {
        if (level < ActiveLevel)
        {
            return;
        }

        // switch (level)
        // {
        //     case Level.T:
        //         Console.ForegroundColor = ConsoleColor.Gray;
        //         break;
        //     case Level.W:
        //         Console.ForegroundColor = ConsoleColor.Yellow;
        //         break;
        //     case Level.E:
        //         Console.ForegroundColor = ConsoleColor.Red;
        //         break;
        //     default:
        //         break;
        // }

        DateTime now = DateTime.Now;
        string nowString = now.ToString("dd.MM. HH:mm:ss.fff");
        string threadId = Thread.CurrentThread.ManagedThreadId.ToString().PadRight(3);
        string method = callerMethod.PadRight(20).Substring(0, 20);

        string logMessage = nowString + $" {threadId} {method} {level}: {message}";

        if (LogConsole)
        {
            Console.WriteLine($"{threadId} {method} {level}: {message}");
        }
        else
        {

            File.AppendAllText(GetLogFile(now), logMessage);
        }
        
        // Console.ResetColor();
    }

    public static Level ActiveLevel { get; set; } = Log.Level.I;

    public static string GetLogFile(DateTime now)
    {
        string file = $"{(now.ToString("yyyyMMdd"))}.log";

        return Path.Combine(Program.InputArgs.LogDirectory, file);
    }
}