using _3XSyncHub.Models.Enums; 

namespace _3XSyncHub.Models
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Model.AppConfig.cs
    // 🔖 Version: 20251119_0948
    // 📌 Nội dung đã xử lý:
    // - Định nghĩa toàn bộ cấu hình trung tâm của ứng dụng (Config.json)
    // - Tách nhóm cấu hình: Database Master/Slave, DashboardSYT, Autoupdate
    // - Cung cấp mô hình dữ liệu để các Service đọc/ghi cấu hình thống nhất
    // - Khởi tạo toàn bộ nhóm cấu hình mặc định để tránh null
    // ============================================================================

    /// ⚙️ Cấu hình tổng thể cho ứng dụng (đọc/ghi từ Config.json)
    public class AppConfig
    {
        public DatabaseMasterConfig DatabaseMaster { get; set; } = new();
        public DatabaseSlaveConfig DatabaseSlave { get; set; } = new();

        public DashboardSYTConfig DashboardSYT { get; set; } = new();
        public AutoUpdateConfig Autoupdate { get; set; } = new();
    }

    // ===================================================================
    // 🧩 MASTER / SLAVE
    // ===================================================================

    public class DatabaseMasterConfig
    {
        public string Host { get; set; } = "";
        public string Port { get; set; } = "5432";
        public string Database { get; set; } = "";
        public string User { get; set; } = "postgres";
        public string Password { get; set; } = "";
    }

    public class DatabaseSlaveConfig
    {
        public string Host { get; set; } = "";
        public string Port { get; set; } = "5432";
        public string Database { get; set; } = "";
        public string User { get; set; } = "postgres";
        public string Password { get; set; } = "";
    }

    // ===================================================================
    // 📊 Dashboard SYT
    // ===================================================================

    public class DashboardSYTConfig
    {
        public DatabaseSource DatabaseSource { get; set; } = DatabaseSource.DatabaseMaster;
        public string BaseUrl { get; set; } = "";
        public string ApiPrefix { get; set; } = "";
        public AuthConfig Auth { get; set; } = new();
        public int IntervalMinutes { get; set; } = 15;
        public string? DailyTime { get; set; } = null;
    }

    // ===================================================================
    // 🔑 Auth
    // ===================================================================

    public class AuthConfig
    {
        public string Type { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string TokenUrl { get; set; } = "";
    }
}
