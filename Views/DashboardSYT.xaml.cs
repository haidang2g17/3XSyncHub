using System.Windows;
using System.Windows.Controls;
using _3XSyncHub.Models;
using _3XSyncHub.Services;
using _3XSyncHub.Models.Enums;

namespace _3XSyncHub.Views
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.UI.DashboardSYT.xaml.cs
    // 🔖 Version: 20251119_1228
    // 📌 Nội dung đã xử lý:
    // - View đồng bộ SYT PTO: load cấu hình, validate đơn giản, gọi Service
    // - Không sinh log; toàn bộ log do DashboardSYTService xử lý
    // - Bind Logs realtime qua DataContext = DashboardSYTService
    // - Quản lý trạng thái nút (Lưu – Bắt đầu – Dừng) theo service.Start() / Stop()
    // ============================================================================

    /// DashboardSYT: Giao diện đồng bộ dữ liệu sang SYT PTO
    public partial class DashboardSYT : Page
    {
        private readonly DashboardSYTService service = new();

        public DashboardSYT()
        {
            InitializeComponent();
            DataContext = service; /// ✅ Bind log realtime
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var cfg = ConfigService.Load().DashboardSYT;
                if (cfg == null)
                    return;

                txtUrlApi.Text = cfg.BaseUrl ?? "";
                txtUsername.Text = cfg.Auth?.Username ?? "";
                txtPassword.Password = cfg.Auth?.Password ?? "";
                txtInterval.Text = cfg.IntervalMinutes.ToString();
                txtDailyTime.Text = cfg.DailyTime ?? "";

                // Set combobox selected item to enum value
                cmbDatabaseSource.SelectedItem = cfg.DatabaseSource;
            }
            catch (Exception ex)
            {
                _ = DialogService.ShowError($"Không thể tải cấu hình DashboardSYT:\n{ex.Message}");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newCfg = new DashboardSYTConfig
                {
                    BaseUrl = txtUrlApi.Text.Trim(),
                    ApiPrefix = string.Empty, // nếu sau này thêm UI thì bind vào
                    Auth = new AuthConfig
                    {
                        Username = txtUsername.Text.Trim(),
                        Password = txtPassword.Password
                    },
                    IntervalMinutes = int.TryParse(txtInterval.Text, out var m) ? m : 0,
                    DailyTime = string.IsNullOrWhiteSpace(txtDailyTime.Text) ? null : txtDailyTime.Text.Trim(),

                    // ⭐ Chọn DB Master / Slave
                    DatabaseSource = cmbDatabaseSource.SelectedItem is DatabaseSource ds
                        ? ds
                        : DatabaseSource.DatabaseMaster
                };

                service.SaveConfig(newCfg);
            }
            catch (Exception ex)
            {
                _ = DialogService.ShowError($"Lỗi khi lưu cấu hình:\n{ex.Message}");
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;

            bool success = await service.StartAsync(); // Async chuẩn, không block UI

            if (!success)
            {
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
                _ = DialogService.ShowError("Khởi động Dashboard SYT thất bại!\nXem log để biết chi tiết.");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            service.Stop();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

        /// ✅ Proxy cho MainWindow gọi dừng tiến trình SYT
        public void Stop()
        {
            service.Stop();
        }
    }
}
