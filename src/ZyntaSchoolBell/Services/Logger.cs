using System;
using System.IO;

namespace ZyntaSchoolBell.Services
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logDir;
        private static readonly string _logFile;
        private const long MaxFileSize = 1024 * 1024; // 1MB
        private const int MaxBackups = 3;

        static Logger()
        {
            _logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ZyntaSchoolBell", "logs");
            _logFile = Path.Combine(_logDir, "app.log");
        }

        public static void Info(string message) => Log("INFO", message);
        public static void Warn(string message) => Log("WARN", message);
        public static void Error(string message) => Log("ERROR", message);
        public static void Error(string message, Exception ex) => Log("ERROR", $"{message}: {ex}");

        private static void Log(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(_logDir);
                    RotateIfNeeded();
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logFile, line);
                }
                catch
                {
                    // Logging must never crash the app
                }
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(_logFile)) return;
                var info = new FileInfo(_logFile);
                if (info.Length < MaxFileSize) return;

                for (int i = MaxBackups - 1; i >= 1; i--)
                {
                    string src = _logFile + "." + i;
                    string dst = _logFile + "." + (i + 1);
                    if (File.Exists(dst)) File.Delete(dst);
                    if (File.Exists(src)) File.Move(src, dst);
                }

                string backup1 = _logFile + ".1";
                if (File.Exists(backup1)) File.Delete(backup1);
                File.Move(_logFile, backup1);
            }
            catch
            {
                // Rotation failure is non-critical
            }
        }
    }
}
