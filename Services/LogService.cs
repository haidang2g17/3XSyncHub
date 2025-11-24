using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.LogService.cs
    // 🔖 Version: 20251119_1140
    // 📌 Nội dung đã xử lý:
    // - Ghi log nền theo module: Logs/<Module>_yyyy-MM-dd.log
    // - Thread-safe (lock) + chạy phi đồng bộ (Task.Run)
    // - Dọn log cũ hơn 10 ngày tự động
    // - Format chuẩn: [yyyy-MM-dd HH:mm:ss] [LEVEL] Message
    // ============================================================================

    public static class LogService
    {
        private static readonly object _lock = new();
        private static DateTime _lastCleanupDate = DateTime.MinValue;
        private const int RetentionDays = 10;

        /// Ghi log mức INFO
        public static void Info(string module, string message)
            => WriteAsync(module, "INFO", message);

        /// Ghi log mức SUCCESS
        public static void Success(string module, string message)
            => WriteAsync(module, "SUCCESS", message);

        /// Ghi log mức WARNING
        public static void Warning(string module, string message)
            => WriteAsync(module, "WARN", message);

        /// Ghi log mức ERROR
        public static void Error(string module, string message)
            => WriteAsync(module, "ERROR", message);

        /// Ghi log thực tế (phi đồng bộ)
        private static void WriteAsync(string module, string level, string message)
        {
            try
            {
                Task.Run(() =>
                {
                    lock (_lock)
                    {
                        string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                        if (!Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);

                        string date = DateTime.Now.ToString("yyyy-MM-dd");
                        string filePath = Path.Combine(logDir, $"{module}_{date}.log");

                        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string line = $"[{time}] [{level}] {message}";

                        File.AppendAllText(filePath, line + Environment.NewLine);

                        if (_lastCleanupDate.Date != DateTime.Now.Date)
                        {
                            _lastCleanupDate = DateTime.Now.Date;
                            CleanupOldLogs(logDir);
                        }
                    }
                });
            }
            catch
            {
                // Không throw lỗi ra ngoài để tránh crash luồng ghi log
            }
        }

        /// Xoá file log cũ hơn RetentionDays
        private static void CleanupOldLogs(string logDir)
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-RetentionDays);
                var files = Directory.GetFiles(logDir, "*.log")
                    .Where(f => TryParseDateFromFile(f, out var d) && d < cutoff);

                foreach (var file in files)
                    File.Delete(file);
            }
            catch
            {
                // Bỏ qua lỗi IO khi cleanup
            }
        }

        /// Trích xuất ngày từ tên file dạng: <Module>_yyyy-MM-dd.log
        private static bool TryParseDateFromFile(string filePath, out DateTime date)
        {
            date = DateTime.MinValue;
            try
            {
                string name = Path.GetFileNameWithoutExtension(filePath);
                string[] parts = name.Split('_');
                if (parts.Length < 2)
                    return false;

                return DateTime.TryParse(parts.Last(), out date);
            }
            catch
            {
                return false;
            }
        }
    }
}
