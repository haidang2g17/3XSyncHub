using ModernWpf;
using System;
using System.Windows;
using _3XSyncHub.Services;

namespace _3XSyncHub
{
    // ============================================================================
    // 🗂️ Module: App.xaml.cs
    // 🔖 Version: 20251119_3XSyncHub.Core.App.v1.0
    // 📝 Mục đích:
    //     - Khởi động ứng dụng WPF
    //     - Kiểm tra & tạo Config.json mặc định khi chưa tồn tại
    //     - Xử lý lỗi khởi tạo cấu hình ở mức hệ thống
    // ============================================================================ 

    public partial class App : Application
    {
        /// Khởi động ứng dụng
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            /// Đảm bảo file config tồn tại, nếu chưa có thì tạo mới
            try
            {
                ConfigService.EnsureConfigExists();
#if DEBUG
                Console.WriteLine("✅ Đã kiểm tra Config.json (tạo mới nếu chưa có).");
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo cấu hình: {ex.Message}",
                                "Lỗi hệ thống",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
