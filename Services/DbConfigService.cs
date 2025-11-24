using Npgsql;
using _3XSyncHub.Models;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.DbConfigService
    // 🔖 Version: 20251119_1144
    // 📌 Mục đích:
    // - Lưu cấu hình database Master + Slave
    // - Kiểm tra kết nối cả 2 server
    // - Log UI + log file qua LogView theo chuẩn 3XVN
    // ============================================================================

    public class DbConfigService : LogView
    {
        public void SaveMasterAndSlave(DatabaseMasterConfig master, DatabaseSlaveConfig slave)
        {
            try
            {
                var cfg = ConfigService.Load();

                cfg.DatabaseMaster = master;
                cfg.DatabaseSlave = slave;

                ConfigService.Save(cfg);

                LogSuccess("Đã lưu cấu hình Database Master & Slave.");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi lưu cấu hình: {ex.Message}");
            }
        }

        public void TestMasterSlave(
            string mHost, string mPort, string mUser, string mPass, string mDb,
            string sHost, string sPort, string sUser, string sPass, string sDb)
        {
            TestOne("MASTER", mHost, mPort, mUser, mPass, mDb);
            TestOne("SLAVE", sHost, sPort, sUser, sPass, sDb);
        }

        private void TestOne(string label, string host, string port, string user, string pass, string db)
        {
            try
            {
                var connStr = $"Host={host};Port={port};Username={user};Password={pass};Database={db}";
                using var conn = new NpgsqlConnection(connStr);
                conn.Open();

                LogSuccess($"{label}: Kết nối thành công.");
            }
            catch (Exception ex)
            {
                LogError($"{label}: Kết nối thất bại — {ex.Message}");
            }
        }
    }
}
