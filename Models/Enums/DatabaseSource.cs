using System;

// ============================================================================
// 🗂️ File: Models/Enums/DatabaseSource.cs
// 🔖 Version: v20251119_3XSyncHub.Model.Enums.DatabaseSource.v1.0
// 📌 Mục đích:
// - Cung cấp enum DatabaseSource (Master / Slave) dùng chung toàn hệ thống.
// - Hỗ trợ Dashboard SYT và các module cần chọn nguồn dữ liệu.
// - Tách riêng khỏi AppConfig để tránh lỗi XAMLParseException.
// - Đúng chuẩn cấu trúc Models/Enums của 3XVN.
// ============================================================================

namespace _3XSyncHub.Models.Enums
{
    /// Nguồn Database cho Dashboard SYT & các module khác
    public enum DatabaseSource
    {
        DatabaseMaster,
        DatabaseSlave
    }
}
