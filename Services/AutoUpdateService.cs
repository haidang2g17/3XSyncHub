using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using _3XSyncHub.Models;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.AutoUpdateService.cs
    // 🔖 Version: 20251120_0900
    // 📌 Nội dung đã xử lý:
    // - Xử lý AutoUpdate (HISUpdateStock & EMRUpdateDocument)
    // - Timer nền theo interval + chạy lần đầu ngay lập tức
    // - Kiểm tra DB, kiểm tra file SQL trước khi chạy
    // - Ghi log chuẩn bằng LogView (UI + file theo module)
    // - Fix lỗi LoadSql, tránh log trùng, kiểm soát trạng thái chạy
    // - AutoUpdateService: Xử lý tác vụ tự động (HISUpdateStock, EMRUpdateDocument)
    // ============================================================================

    public class AutoUpdateService : LogView
    {
        private readonly DispatcherTimer hisUpdateTimer = new();
        private readonly DispatcherTimer emrUpdateTimer = new();
        private bool _isStarted = false;
        private bool _hisRunning = false;
        private bool _emrRunning = false;

        /// Trạng thái tiến trình AutoUpdate
        public bool IsRunning => _isStarted;

        /// 👉 EVENT báo trạng thái Running cho MainWindow
        public static event Action<bool>? RunningChanged;

        /// Khởi động AutoUpdate dựa theo cấu hình
        public void Start()
        {
            if (_isStarted)
            {
                LogWarning("AutoUpdate đang chạy, bỏ qua yêu cầu khởi động lại.");
                return;
            }

            try
            {
                if (!DatabaseService.TestConnection())
                {
                    LogError("Không thể khởi động — kết nối Database Master thất bại.");
                    LogWarning("Vui lòng kiểm tra lại cấu hình Datasbase Master trong mục Kết nối Database.");
                    RunningChanged?.Invoke(false);
                    return;
                }

                var config = ConfigService.Load();
                if (config.Autoupdate == null)
                {
                    LogError("Không tìm thấy cấu hình AutoUpdate trong Config.json.");
                    RunningChanged?.Invoke(false);
                    return;
                }

                var auto = config.Autoupdate;
                bool anyStarted = false;
                LogInfo("Đang kiểm tra điều kiện khởi động các tác vụ AutoUpdate...");

                // ===== HISUpdateStock =====
                if (auto.HISUpdateStock)
                {
                    string hisFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "AutoUpdate", "UpdateMedicineStoreSoluongKhadung.sql");
                    if (!File.Exists(hisFile))
                    {
                        LogError($"Không thể khởi động HISUpdateStock — thiếu file SQL: \\Sql\\AutoUpdate\\UpdateMedicineStoreSoluongKhadung.sql");
                    }
                    else
                    {
                        StartHISUpdateStock(auto.HISUpdateStockInterval);
                        _ = ExecuteHISUpdateStock();
                        anyStarted = true;
                    }
                }

                // ===== EMRUpdateDocument =====
                if (auto.EMRUpdateDocument)
                {
                    string emrFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "AutoUpdate", "UpdateEmrDocumentError.sql");
                    if (!File.Exists(emrFile))
                    {
                        LogError($"Không thể khởi động EMRUpdateDocument — thiếu file SQL: \\Sql\\AutoUpdate\\UpdateEmrDocumentError.sql");
                    }
                    else
                    {
                        StartEMRUpdateDocument(auto.EMRUpdateDocumentInterval);
                        _ = ExecuteEMRUpdateDocument();
                        anyStarted = true;
                    }
                }

                if (anyStarted)
                {
                    _isStarted = true;
                    RunningChanged?.Invoke(true);        // 🔥 BLINK START
                }
                else
                {
                    LogWarning("Không có tác vụ nào được khởi động (thiếu file SQL hoặc chưa bật checkbox).");
                    RunningChanged?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi khởi động AutoUpdate: {ex.Message}");
                RunningChanged?.Invoke(false);
            }
        }

        /// Dừng toàn bộ AutoUpdate
        public void Stop()
        {
            try
            {
                if (!_isStarted)
                {
                    LogInfo("AutoUpdate hiện đang dừng, không cần dừng lại.");
                    RunningChanged?.Invoke(false);
                    return;
                }

                // 🟦 QUAN TRỌNG: GỠ SỰ KIỆN TRÁNH DOUBLE-TICK SAU NHIỀU LẦN START/STOP
                hisUpdateTimer.Tick -= HisUpdate_Tick;
                emrUpdateTimer.Tick -= EmrUpdate_Tick;

                hisUpdateTimer.Stop();
                emrUpdateTimer.Stop();

                _isStarted = false;
                _hisRunning = false;
                _emrRunning = false;

                RunningChanged?.Invoke(false);        // 🔥 TẮT BLINK

                LogInfo("Đã dừng HISUpdateStock timer.");
                LogInfo("Đã dừng EMRUpdateDocument timer.");
                LogWarning("Đã dừng toàn bộ tác vụ AutoUpdate.");
                LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi dừng AutoUpdate: {ex.Message}");
                RunningChanged?.Invoke(false);
            }
        }

        #region HISUpdateStock

        private void StartHISUpdateStock(int intervalMinutes)
        {
            try
            {
                hisUpdateTimer.Tick -= HisUpdate_Tick;
                hisUpdateTimer.Tick += HisUpdate_Tick;
                hisUpdateTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                hisUpdateTimer.Start();

                LogInfo($"Đã khởi động HISUpdateStock (mỗi {intervalMinutes} phút).");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khởi động HISUpdateStock: {ex.Message}");
            }
        }

        private async void HisUpdate_Tick(object? sender, EventArgs e)
        {
            await ExecuteHISUpdateStock();
        }

        private async Task ExecuteHISUpdateStock()
        {
            if (_hisRunning)
            {
                LogWarning("HISUpdateStock vẫn đang chạy, bỏ qua tick này.");
                return;
            }

            _hisRunning = true;
            try
            {
                string sql = LoadSql("UpdateMedicineStoreSoluongKhadung.sql");
                if (string.IsNullOrEmpty(sql))
                    return;

                int rows = await DatabaseService.ExecuteNonQueryAsync(sql);
                LogSuccess($"HISUpdateStock hoàn tất — Đã cập nhật {rows} dòng.");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi HISUpdateStock: {ex.Message}");
            }
            finally
            {
                _hisRunning = false;
            }
        }

        #endregion

        #region EMRUpdateDocument

        private void StartEMRUpdateDocument(int intervalMinutes)
        {
            try
            {
                emrUpdateTimer.Tick -= EmrUpdate_Tick;
                emrUpdateTimer.Tick += EmrUpdate_Tick;
                emrUpdateTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                emrUpdateTimer.Start();

                LogInfo($"Đã khởi động EMRUpdateDocument (mỗi {intervalMinutes} phút).");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khởi động EMRUpdateDocument: {ex.Message}");
            }
        }

        private async void EmrUpdate_Tick(object? sender, EventArgs e)
        {
            await ExecuteEMRUpdateDocument();
        }

        private async Task ExecuteEMRUpdateDocument()
        {
            if (_emrRunning)
            {
                LogWarning("EMRUpdateDocument vẫn đang chạy, bỏ qua tick này.");
                return;
            }

            _emrRunning = true;
            try
            {
                string sql = LoadSql("UpdateEmrDocumentError.sql");
                if (string.IsNullOrEmpty(sql))
                    return;

                int rows = await DatabaseService.ExecuteNonQueryAsync(sql);
                LogSuccess($"EMRUpdateDocument hoàn tất — Đã xử lý {rows} dòng.");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi EMRUpdateDocument: {ex.Message}");
            }
            finally
            {
                _emrRunning = false;
            }
        }

        #endregion

        #region Helper

        private string LoadSql(string fileName)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "AutoUpdate", fileName);
                if (!File.Exists(path))
                {
                    LogError($"Không tìm thấy file SQL: \\Sql\\AutoUpdate\\{fileName}");
                    return string.Empty;
                }

                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi đọc file SQL: {ex.Message}");
                return string.Empty;
            }
        }

        public void SaveConfig(AutoUpdateConfig config)
        {
            try
            {
                var appConfig = ConfigService.Load();
                appConfig.Autoupdate = config;
                ConfigService.Save(appConfig);
                LogSuccess("Đã lưu cấu hình AutoUpdate thành công.");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi lưu cấu hình AutoUpdate: {ex.Message}");
            }
        }

        #endregion
    }
}
