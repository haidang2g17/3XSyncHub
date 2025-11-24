using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace _3XSyncHub.Models
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Model.LogView.cs
    // 🔖 Version: 20251119_1010
    // 📌 Nội dung đã xử lý:
    // - Quản lý danh sách log hiển thị UI (ObservableCollection)
    // - Ghi log tự động ra file theo module (1 file/ngày)
    // - Thread-safe UI update, giới hạn 200 dòng
    // - Dùng bởi toàn bộ Service kế thừa LogView
    // ============================================================================

    /// LogView: Quản lý danh sách log để UI binding và ghi log ra file
    public class LogView
    {
        /// Danh sách log hiển thị trên UI (log mới nhất đứng đầu)
        public ObservableCollection<LogItem> Logs { get; } = new();

        /// Ghi log mức Info
        public void LogInfo(string message) => AddLog("ℹ️ " + message, Colors.DodgerBlue);

        /// Ghi log mức Success
        public void LogSuccess(string message) => AddLog("✅ " + message, Colors.ForestGreen);

        /// Ghi log mức Warning
        public void LogWarning(string message) => AddLog("⚠️ " + message, Colors.OrangeRed);

        /// Ghi log mức Error
        public void LogError(string message) => AddLog("❌ " + message, Colors.Red);

        /// Thêm log mới (hiển thị UI + ghi file nền)
        private void AddLog(string message, Color color)
        {
            try
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string formattedMessage = $"[{time}] {message}";

                var item = new LogItem
                {
                    Time = time,
                    Message = formattedMessage,
                    Color = new SolidColorBrush(color)
                };

                // Cập nhật UI thread-safe
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    Logs.Insert(0, item);
                    if (Logs.Count > 200)
                        Logs.RemoveAt(Logs.Count - 1);
                });

                // Ghi file nền (non-blocking)
                Task.Run(() => WriteLogToFile(formattedMessage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogView Error] {ex.Message}");
            }
        }

        /// Ghi log ra file theo module (1 file/ngày/module)
        private void WriteLogToFile(string message)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string moduleName = GetModuleName();
                string logFile = Path.Combine(logDir, $"{moduleName}_{DateTime.Now:yyyyMMdd}.txt");

                // Ghi nối tiếp UTF-8, không dòng trống
                File.AppendAllText(logFile, message + Environment.NewLine, Encoding.UTF8);

                CleanupOldLogs(logDir, moduleName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WriteLogToFile Error] {ex.Message}");
            }
        }

        /// Xóa file log cũ hơn 10 ngày
        private void CleanupOldLogs(string logDir, string moduleName)
        {
            try
            {
                var files = Directory.GetFiles(logDir, $"{moduleName}_*.txt");
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    if (info.CreationTime < DateTime.Now.AddDays(-10))
                        info.Delete();
                }
            }
            catch
            {
                // Bỏ qua lỗi file bị khóa
            }
        }

        /// Xác định tên module dựa theo class kế thừa LogView
        private string GetModuleName()
        {
            try
            {
                var type = GetType().Name.Replace("Service", "");
                return string.IsNullOrWhiteSpace(type) ? "General" : type;
            }
            catch
            {
                return "General";
            }
        }
    }

    /// LogItem: Mẫu log cho UI ListBox binding
    public class LogItem
    {
        public string Time { get; set; } = "";
        public string Message { get; set; } = "";
        public SolidColorBrush Color { get; set; } = new(Colors.Black);
    }
}
