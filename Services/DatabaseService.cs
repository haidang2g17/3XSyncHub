using System;
using System.Threading.Tasks;
using Npgsql;
using _3XSyncHub.Models;
using _3XSyncHub.Models.Enums;


namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.DatabaseService.cs
    // 🔖 Version: 20251119_1122
    // 📌 Nội dung đã xử lý:
    // - Build connection string cho DatabaseMaster / DatabaseSlave
    // - Cung cấp API truy vấn: ExecuteNonQueryAsync, ExecuteScalarAsync
    // - Test kết nối DB Master (fail-fast cho toàn hệ thống)
    // - Hỗ trợ DashboardSYT chọn nguồn DB theo DatabaseSource
    // ============================================================================

    public static class DatabaseService
    {
        // ================================================================
        // 🔥 HỖ TRỢ MASTER / SLAVE
        // ================================================================

        private static string BuildConnStr(dynamic db)
            => $"Host={db.Host};Port={db.Port};Username={db.User};Password={db.Password};Database={db.Database}";

        /// Lấy connection string theo enum (DashboardSYT sẽ gọi hàm này)
        public static string GetConnectionString(DatabaseSource source)
        {
            var config = ConfigService.Load();

            return source switch
            {
                DatabaseSource.DatabaseMaster => BuildConnStr(config.DatabaseMaster),
                DatabaseSource.DatabaseSlave => BuildConnStr(config.DatabaseSlave),
                _ => BuildConnStr(config.DatabaseMaster)
            };
        }

        /// Kết nối Master (mặc định cho CRUD)
        private static NpgsqlConnection GetMasterConnection()
        {
            var cfg = ConfigService.Load().DatabaseMaster;
            return new NpgsqlConnection(BuildConnStr(cfg));
        }

        /// Kết nối Slave (dùng cho DashboardSYT)
        public static NpgsqlConnection GetSlaveConnection()
        {
            var cfg = ConfigService.Load().DatabaseSlave;
            return new NpgsqlConnection(BuildConnStr(cfg));
        }

        // ================================================================
        // 🔄 CRUD — Vẫn chạy trên MASTER
        // ================================================================

        public static async Task<int> ExecuteNonQueryAsync(string sql)
        {
            try
            {
                await using var conn = GetMasterConnection();
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                int rows = await cmd.ExecuteNonQueryAsync();
                LogService.Success("DatabaseService", $"Thực thi SQL thành công ({rows} dòng).");
                return rows;
            }
            catch (Exception ex)
            {
                LogService.Error("DatabaseService", $"Lỗi khi thực thi SQL: {ex.Message}");
                return 0;
            }
        }

        public static async Task<object?> ExecuteScalarAsync(string sql)
        {
            try
            {
                await using var conn = GetMasterConnection();
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                var result = await cmd.ExecuteScalarAsync();
                LogService.Info("DatabaseService", "Thực thi truy vấn đơn thành công.");
                return result;
            }
            catch (Exception ex)
            {
                LogService.Error("DatabaseService", $"Lỗi khi thực thi truy vấn đơn: {ex.Message}");
                return null;
            }
        }

        // ================================================================
        // 🔧 Kiểm tra kết nối — Test MASTER
        // ================================================================

        public static bool TestConnection()
        {
            try
            {
                using var conn = GetMasterConnection();
                conn.Open();
                conn.Close();

                LogService.Info("DatabaseService", "Kiểm tra kết nối DatabaseMaster thành công.");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Error("DatabaseService", $"Lỗi kiểm tra kết nối DB Master: {ex.Message}");
                return false;
            }
        }
    }
}
