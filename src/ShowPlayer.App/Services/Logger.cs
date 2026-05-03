using System.IO;

namespace ShowPlayer.App.Services
{
    public class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShowPlayer", "log.txt");

        private static readonly object LockObj = new();

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            var msg = ex != null ? $"{message} | Exception: {ex.Message}" : message;
            WriteLog("ERROR", msg);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                lock (LockObj)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
            }
        }
    }
}
