using _3XSyncHub.Services;
using _3XSyncHub.Views;
using _3XSyncHub.Views.Dialogs;
using ModernWpf.Controls;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace _3XSyncHub
{
    // ============================================================================
    // 🗂️ Module: MainWindow.xaml.cs
    // 🔖 Version: 20251120_0900
    // 🧩 Chuẩn 3XVN.UI v7.6.x — Kiểm soát NavigationView + Load page an toàn
    // 📝 View không log, chỉ gọi Service. Hỗ trợ lazy-init form & confirm exit.
    // ============================================================================

    public partial class MainWindow : Window
    {
        /// Giữ instance các form (lazy init – tạo khi mở lần đầu)
        private HomePage? homepage;
        private DbConfig? dbconfig;
        private DashboardSYT? dashboardsyt;
        private AutoUpdate? autoupdate;

        private DispatcherTimer? _blinkDashboardTimer;
        private DispatcherTimer? _blinkAutoUpdateTimer;

        private bool _toggleDashboard;
        private bool _toggleAutoUpdate;

        private readonly Brush _normalColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2A33"));
        private readonly Brush _blinkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D7"));


        public MainWindow()
        {
            InitializeComponent();
            DashboardSYTService.RunningChanged += DashboardSYTService_RunningChanged;
            AutoUpdateService.RunningChanged += AutoUpdateService_RunningChanged;

            /// ✅ Kiểm tra & tạo file cấu hình nếu chưa có
            try
            {
                ConfigService.EnsureConfigExists();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo file cấu hình: {ex.Message}",
                                "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            /// 👉 Hiển thị mặc định Trang chủ
            NavView.SelectedItem = NavView.MenuItems[0];
            ShowPage("home");
        }

        /// ✅ Xác nhận thoát phần mềm
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            var dialog = new ClassicDialog(
                ClassicDialog.DialogType.Confirm,
                "Bạn có chắc muốn thoát phần mềm không?",
                "Xác nhận thoát");

            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.Result)
                Application.Current.Shutdown();
        }

        /// ✅ Xử lý chuyển form theo menu
        private async void NavView_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is not NavigationViewItem item)
                return;

            string? tag = item.Tag?.ToString()?.ToLower();
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (tag == "phieu")
            {
                await DialogService.ShowInfo("Chức năng đang được phát triển.");
                return;
            }

            ShowPage(tag);
        }

        /// ✅ Hàm hiển thị form (lazy-init)
        private void ShowPage(string tag)
        {
            switch (tag)
            {
                case "home":
                    homepage ??= new HomePage();
                    ContentFrame.Content = homepage;
                    break;

                case "dbconfig":
                    dbconfig ??= new DbConfig();
                    ContentFrame.Content = dbconfig;
                    break;

                case "dashboardsyt":
                    dashboardsyt ??= new DashboardSYT();
                    ContentFrame.Content = dashboardsyt;
                    break;

                case "autoupdate":
                    autoupdate ??= new AutoUpdate();
                    ContentFrame.Content = autoupdate;
                    break;
            }
        }

        // ============================================================================
        // ⬇️ BỔ SUNG — BLINK ICON MENU CHO dashboardsyt & autoupdate
        // ============================================================================

        private void DashboardSYTService_RunningChanged(bool isRunning)
        {
            if (isRunning)
                StartBlinkDashboardIcon();
            else
                StopBlinkDashboardIcon();
        }

        private void AutoUpdateService_RunningChanged(bool isRunning)
        {
            if (isRunning)
                StartBlinkAutoUpdateIcon();
            else
                StopBlinkAutoUpdateIcon();
        }

        // ------------------- DASHBOARD SYT ------------------------------------

        private void OnDashboardBlink(object? sender, EventArgs e)
        {
            _toggleDashboard = !_toggleDashboard;
            SetMenuIconColor("dashboardsyt", _toggleDashboard ? _blinkColor : _normalColor);
        }

        private void StartBlinkDashboardIcon()
        {
            _blinkDashboardTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };

            _blinkDashboardTimer.Tick -= OnDashboardBlink;   // tránh Tick trùng
            _blinkDashboardTimer.Tick += OnDashboardBlink;

            _blinkDashboardTimer.Start();
        }

        private void StopBlinkDashboardIcon()
        {
            if (_blinkDashboardTimer == null) return;

            _blinkDashboardTimer.Tick -= OnDashboardBlink;
            _blinkDashboardTimer.Stop();

            _toggleDashboard = false;
            SetMenuIconColor("dashboardsyt", _normalColor);
        }

        // ------------------- AUTOUPDATE ----------------------------------------

        private void OnAutoUpdateBlink(object? sender, EventArgs e)
        {
            _toggleAutoUpdate = !_toggleAutoUpdate;
            SetMenuIconColor("autoupdate", _toggleAutoUpdate ? _blinkColor : _normalColor);
        }

        private void StartBlinkAutoUpdateIcon()
        {
            _blinkAutoUpdateTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };

            _blinkAutoUpdateTimer.Tick -= OnAutoUpdateBlink;
            _blinkAutoUpdateTimer.Tick += OnAutoUpdateBlink;

            _blinkAutoUpdateTimer.Start();
        }

        private void StopBlinkAutoUpdateIcon()
        {
            if (_blinkAutoUpdateTimer == null) return;

            _blinkAutoUpdateTimer.Tick -= OnAutoUpdateBlink;
            _blinkAutoUpdateTimer.Stop();

            _toggleAutoUpdate = false;
            SetMenuIconColor("autoupdate", _normalColor);
        }

        // ------------------- CORE: ĐỔI MÀU ICON THEO TAG -----------------------

        private void SetMenuIconColor(string tag, Brush color)
        {
            var item = NavView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(x => (string)x.Tag == tag);

            if (item == null) return;

            var symbol = FindChild<SymbolIcon>(item);
            if (symbol != null)
                symbol.Foreground = color;
        }

        // ------------------- HELPER: TÌM SYMBOLICON TRONG NAVVIEW ITEM ---------

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    return typedChild;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        // ============================================================================
        // ⬆️ KẾT THÚC BỔ SUNG — BLINK ICON MENU
        // ============================================================================
    }
}
