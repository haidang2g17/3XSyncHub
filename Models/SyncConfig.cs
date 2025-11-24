using System.Collections.Generic;

namespace _3XSyncHub.Models
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Model.SyncConfig.cs
    // 🔖 Version: 20251119_1016
    // 📌 Nội dung đã xử lý:
    // - Mô hình cấu hình cho module SyncService (đa kênh đồng bộ)
    // - Quản lý danh sách các channel: SYTPTO, DượcQG, EMR, HIS, ...
    // - Mỗi channel có URL, API prefix, auth header, access key, SQL folder riêng
    // - Thuần dữ liệu, dùng để SyncService load & chạy tác vụ đồng bộ
    // ============================================================================

    /// SyncConfig: Mô hình cấu hình cho module SyncService (đa kênh)
    public class SyncConfig
    {
        /// Danh sách các kênh đồng bộ (mỗi kênh có cấu hình riêng)
        public List<SyncChannel> Channels { get; set; } = new();
    }

    /// SyncChannel: Cấu hình chi tiết cho từng kênh đồng bộ
    public class SyncChannel
    {
        /// Tên định danh kênh (ví dụ: "SYTPTO", "DuocQG")
        public string Name { get; set; } = "";

        /// URL gốc của API
        public string BaseUrl { get; set; } = "";

        /// Tiền tố endpoint (ví dụ: "api/drug", "bireport/v1")
        public string ApiPrefix { get; set; } = "";

        /// Tên header xác thực (ví dụ: "Authorization", "X-APP-ACCESS-KEY")
        public string AuthHeader { get; set; } = "";

        /// Giá trị token/key xác thực (ví dụ: "Bearer xxx", "abc123")
        public string AccessKey { get; set; } = "";

        /// Thư mục chứa các file SQL riêng cho kênh này
        public string SqlFolder { get; set; } = "";
    }
}
