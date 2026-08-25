using System.Text;

namespace LmStudioServerAdmin.Logging;

public static class Logger
{
    private static readonly string _logFilePath;
    private static readonly object _lock = new();

    static Logger()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logsDir = Path.Combine(baseDir, "logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);
        _logFilePath = Path.Combine(logsDir, "app.log");
    }

    public static void Info(string message)
    {
        Log("INFO", message);
    }

    public static void Warning(string message)
    {
        Log("WARN", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message} — {ex}" : message;
        Log("ERROR", fullMessage);
    }

    private static void Log(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var line = $"[{timestamp}] [{level}] {message}";
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Фатально — если лог не пишется, ничего не делаем
            }
        }
    }
}
