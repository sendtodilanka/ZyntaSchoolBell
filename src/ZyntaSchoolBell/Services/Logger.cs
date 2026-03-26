using System;
using System.IO;

namespace ZyntaSchoolBell.Services
{
    public static class Logger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZyntaSchoolBell", "logs");

        private static readonly string LogFile = Path.Combine(LogDir, "app.log");
        private static readonly object Lock = new object();
        private const long MaxFileSize = 1 * 1024 * 1024; // 1 MB
        private const int MaxBackups = 3;

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Error(string message, Exception ex) => Write("ERROR", $"{message}: {ex}");

        private static void Write(string level, string message)
        {
            lock (Lock)
            {
                try
                {
                    Directory.CreateDirectory(LogDir);
                    RotateIfNeeded();

                    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFile, line, System.Text.Encoding.UTF8);
                }
                catch
                {
                    // Logging must never crash the app
                }
            }
        }

        private static void RotateIfNeeded()
        {
            if (!File.Exists(LogFile)) return;
            var info = new FileInfo(LogFile);
            if (info.Length < MaxFileSize) return;

            for (int i = MaxBackups - 1; i >= 1; i--)
            {
                var src = $"{LogFile}.{i}";
                var dst = $"{LogFile}.{i + 1}";
                if (File.Exists(dst)) File.Delete(dst);
                if (File.Exists(src)) File.Move(src, dst);
            }

            var backup1 = $"{LogFile}.1";
            if (File.Exists(backup1)) File.Delete(backup1);
            File.Move(LogFile, backup1);
        }
    }
}
