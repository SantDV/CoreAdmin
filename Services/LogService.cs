using System;
using System.IO;

namespace CoreAdmin.Services;

public class LogService
{
    private const string LogFile = "error_log.txt";

    public void LogError(string message, Exception? ex = null)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n";
            if (ex != null)
            {
                logMessage += $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}\n";
            }
            logMessage += "--------------------------------------------------\n";

            File.AppendAllText(LogFile, logMessage);
        }
        catch
        {
            // Fallback silencioso si no se puede escribir el log
        }
    }

    public void LogInfo(string message)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n";
            File.AppendAllText(LogFile, logMessage);
        }
        catch { }
    }
}
