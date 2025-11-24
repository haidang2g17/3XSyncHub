using System;

namespace _3XSyncHub.Models
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Model.DataJob.cs
    // 🔖 Version: 20251119_1002
    // 📌 Nội dung đã xử lý:
    // - Mô tả cấu trúc 1 job đồng bộ dữ liệu đơn lẻ
    // - Đại diện cho tác vụ: 1 file SQL → 1 API endpoint
    // - Dùng bởi SyncService & DashboardSYTService để thực thi tuần tự
    // - Thuần dữ liệu, không chứa logic xử lý
    // ============================================================================

    /// DataJob: Mô tả một công việc đồng bộ dữ liệu (1 file SQL → 1 API endpoint)
    public class DataJob
    {
        /// Tên job (dùng cho log, ví dụ: "SendDrugList")
        public string Name { get; set; } = string.Empty;

        /// Nội dung SQL cần thực thi
        public string Sql { get; set; } = string.Empty;

        /// Đường dẫn endpoint API (ví dụ: "/api/drug/sync")
        public string Endpoint { get; set; } = string.Empty;
    }
}
