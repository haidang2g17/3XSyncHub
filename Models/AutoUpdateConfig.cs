using System;

namespace _3XSyncHub.Models
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Model.AutoUpdateConfig.cs
    // 🔖 Version: 20251119_0958
    // 📌 Nội dung đã xử lý:
    // - Cấu hình cho chức năng AutoUpdate toàn hệ thống (HIS, EMR, Meta)
    // - Định nghĩa tham số vận hành: bật/tắt, interval, meta info
    // - Model trung tâm để AutoUpdateService đọc/ghi cấu hình
    // - Lưu trong Config.json, khởi tạo mặc định đầy đủ
    // ============================================================================

    /// AutoUpdateConfig: Cấu hình cho chức năng AutoUpdate (đọc/ghi từ Config.json)
    public class AutoUpdateConfig
    {
        /// Bật / Tắt chức năng cập nhật số lượng khả dụng trong kho (HIS)
        public bool HISUpdateStock { get; set; } = true;

        /// Bật / Tắt chức năng đồng bộ lại tài liệu EMR lỗi
        public bool EMRUpdateDocument { get; set; } = false;

        /// Khoảng thời gian giữa 2 lần chạy HISUpdateStock (phút)
        public int HISUpdateStockInterval { get; set; } = 5;

        /// Khoảng thời gian giữa 2 lần chạy EMRUpdateDocument (phút)
        public int EMRUpdateDocumentInterval { get; set; } = 1440;

        /// Phiên bản cấu hình (tùy chọn meta)
        public string Version { get; set; } = string.Empty;

        /// URL liên quan đến bản cập nhật (nếu có)
        public string Url { get; set; } = string.Empty;

        /// Ngày tạo bản cấu hình
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// Trạng thái kích hoạt cấu hình
        public bool Active { get; set; } = true;
    }
}
