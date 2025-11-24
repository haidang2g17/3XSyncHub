using System;
using System.Windows;
using System.Windows.Controls;
using _3XSyncHub.Models;
using _3XSyncHub.Services;

// ============================================================================
// 🗂️ Module: 3XSyncHub.UI.AutoUpdate.xaml.cs
// 🔖 Version: 20251119_1216
// 📌 Nội dung đã xử lý:
// - View điều khiển AutoUpdate: load config, validate input, gọi Service
// - Không sinh log; toàn bộ log do AutoUpdateService xử lý qua LogView
// - Bind Logs realtime bằng DataContext = AutoUpdateService
// - Quản lý trạng thái nút (Lưu – Bắt đầu – Dừng) theo service.IsRunning
// ============================================================================


namespace _3XSyncHub.Views
{
    /// AutoUpdate: Giao diện cấu hình và điều khiển tác vụ tự động (HIS & EMR)
    public partial class AutoUpdate : Page
    {
        private readonly AutoUpdateService service = new();

        public AutoUpdate()
        {
            InitializeComponent();
            DataContext = service; /// ✅ Bind log realtime (LogView)
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ConfigService.Load().Autoupdate;

                chkHISUpdateStock.IsChecked = config.HISUpdateStock;
                txtHISInterval.Text = config.HISUpdateStockInterval.ToString();

                chkEMRUpdateDocument.IsChecked = config.EMRUpdateDocument;
                txtEMRInterval.Text = config.EMRUpdateDocumentInterval.ToString();
            }
            catch
            {
                _ = DialogService.ShowError("Không thể tải cấu hình AutoUpdate.");
            }
        }

        /// Lưu cấu hình AutoUpdate (View chỉ xử lý input)
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool hisEnabled = chkHISUpdateStock.IsChecked == true;
                bool emrEnabled = chkEMRUpdateDocument.IsChecked == true;

                if (hisEnabled && (!int.TryParse(txtHISInterval.Text, out var hisInterval) || hisInterval <= 0))
                {
                    await DialogService.ShowError("Khoảng thời gian HIS phải ≥ 1 phút.");
                    return;
                }

                if (emrEnabled && (!int.TryParse(txtEMRInterval.Text, out var emrInterval) || emrInterval <= 0))
                {
                    await DialogService.ShowError("Khoảng thời gian EMR phải ≥ 1 phút.");
                    return;
                }

                var config = new AutoUpdateConfig
                {
                    HISUpdateStock = hisEnabled,
                    HISUpdateStockInterval = hisEnabled ? int.Parse(txtHISInterval.Text) : 0,
                    EMRUpdateDocument = emrEnabled,
                    EMRUpdateDocumentInterval = emrEnabled ? int.Parse(txtEMRInterval.Text) : 0
                };

                service.SaveConfig(config); /// ✅ Service tự log
            }
            catch (Exception ex)
            {
                await DialogService.ShowError($"Lưu cấu hình thất bại:\n{ex.Message}");
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                service.Start(); /// ✅ Service tự log
                btnStart.IsEnabled = !service.IsRunning;
                btnStop.IsEnabled = service.IsRunning;
            }
            catch (Exception ex)
            {
                _ = DialogService.ShowError($"Lỗi khi khởi động AutoUpdate:\n{ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                service.Stop(); /// ✅ Service tự log
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
            }
            catch (Exception ex)
            {
                _ = DialogService.ShowError($"Lỗi khi dừng AutoUpdate:\n{ex.Message}");
            }
        }

        /// ✅ Proxy cho MainWindow gọi dừng tiến trình AutoUpdate khi thoát
        public void Stop() => service.Stop();

        private void chkEMRUpdateDocument_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
