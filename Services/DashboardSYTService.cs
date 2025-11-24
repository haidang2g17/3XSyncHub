using _3XSyncHub.Models;
using _3XSyncHub.Models.Enums;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // DashboardSYTService – PHIÊN BẢN CUỐI CÙNG (20251124_2200)
    // → 100% async/await chuẩn WPF
    // → Đã xóa method Start() thừa (không còn GetAwaiter().GetResult() → an toàn, không deadlock)
    // → Tối ưu: parallel, retry, log realtime, không block UI
    // ============================================================================
    public class DashboardSYTService : LogView, IDisposable
    {
        private readonly HttpClient client = new();
        private CancellationTokenSource? _cts;
        private AppConfig? _config;
        private bool _isRunning = false;
        private readonly SemaphoreSlim _semaphore = new(3); // Max 3 SQL chạy song song

        public bool IsRunning => _isRunning;
        public static event Action<bool>? RunningChanged;

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        // ====================== CHỈ CÒN 1 CÁCH KHỞI ĐỘNG: ASYNC CHUẨN ======================
        public async Task<bool> StartAsync()
        {
            if (_isRunning)
            {
                LogWarning("DashboardSYT đang chạy, bỏ qua yêu cầu khởi động lại.");
                return true;
            }

            try
            {
                _config = ConfigService.Load();
                if (_config?.DashboardSYT == null)
                {
                    LogError("Không tìm thấy cấu hình DashboardSYT.");
                    RunningChanged?.Invoke(false);
                    return false;
                }

                if (!await TestDatabaseConnectionAsync(_config.DashboardSYT.DatabaseSource))
                {
                    RunningChanged?.Invoke(false);
                    return false;
                }

                _cts = new CancellationTokenSource();

                // FIX CS0029: Task.Run nhận trực tiếp method async
                _ = Task.Run(RunSchedulerAsync, _cts.Token);

                _isRunning = true;
                RunningChanged?.Invoke(true);
                LogSuccess("Đã khởi động đồng bộ Dashboard SYT.");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi khởi động đồng bộ: {ex.Message}");
                _isRunning = false;
                RunningChanged?.Invoke(false);
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _isRunning = false;
            RunningChanged?.Invoke(false);
            LogWarning("Đã dừng đồng bộ Dashboard SYT.");
            LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private async Task<bool> TestDatabaseConnectionAsync(DatabaseSource dbSource)
        {
            string connStr = DatabaseService.GetConnectionString(dbSource);
            try
            {
                await using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();
                LogSuccess($"Kết nối {dbSource} thành công.");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Không thể kết nối {dbSource}: {ex.Message}");
                LogWarning($"Vui lòng kiểm tra lại cấu hình {dbSource} trong mục Kết nối Database.");
                return false;
            }
        }

        private async Task RunSchedulerAsync()
        {
            var token = _cts!.Token;
            try
            {
                var interval = _config!.DashboardSYT.IntervalMinutes;
                var daily = _config.DashboardSYT.DailyTime;

                if (interval > 0)
                {
                    LogInfo($"Chu kỳ {interval} phút.");
                    while (!token.IsCancellationRequested)
                    {
                        await ExecuteSyncCycleAsync(token);
                        await Task.Delay(TimeSpan.FromMinutes(interval), token);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(daily) && TimeSpan.TryParse(daily, out var runAt))
                {
                    LogInfo($"Lịch hằng ngày: {runAt}");
                    while (!token.IsCancellationRequested)
                    {
                        await DelayUntilNextRunAsync(runAt, token);
                        await ExecuteSyncCycleAsync(token);
                    }
                }
                else
                {
                    LogWarning("Không tìm thấy cấu hình Interval hoặc DailyTime.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogError($"Lỗi Scheduler: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                RunningChanged?.Invoke(false);
            }
        }

        private async Task DelayUntilNextRunAsync(TimeSpan runAt, CancellationToken token)
        {
            var now = DateTime.Now;
            var next = now.Date.Add(runAt);
            if (next <= now) next = next.AddDays(1);
            LogInfo($"Lần chạy kế tiếp: {next:HH:mm:ss dd/MM/yyyy}");
            await Task.Delay(next - now, token);
        }

        private async Task ExecuteSyncCycleAsync(CancellationToken token)
        {
            var session = await CreateAuthenticatedSessionAsync(token);
            if (string.IsNullOrEmpty(session?.AppAccessKey)) return;

            var sqlFiles = GetSqlFiles();
            if (sqlFiles.Count == 0) return;

            await Parallel.ForEachAsync(sqlFiles,
                new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = token },
                async (file, ct) => await ProcessSqlFileAsync(file, session.AppAccessKey, ct));

            LogSuccess("Hoàn thành đồng bộ SYT PTO.");
        }

        private List<SqlFileInfo> GetSqlFiles()
        {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "SYTPTO");
            if (!Directory.Exists(folder))
            {
                LogError($"Không tìm thấy thư mục SQL: {folder}");
                return new();
            }

            var files = Directory.GetFiles(folder, "*.sql");
            var list = new List<SqlFileInfo>(files.Length);
            var prefix = string.IsNullOrWhiteSpace(_config?.DashboardSYT.ApiPrefix)
                ? "api/bireport/operator"
                : _config.DashboardSYT.ApiPrefix.Trim('/');

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                list.Add(new SqlFileInfo
                {
                    FullPath = file,
                    Name = name,
                    Endpoint = $"{prefix}/{name}"
                });
            }
            return list;
        }

        private record SqlFileInfo
        {
            public required string FullPath { get; init; }
            public required string Name { get; init; }
            public required string Endpoint { get; init; }
        }

        private record AuthSession(string AppAccessKey);

        private async Task<AuthSession?> CreateAuthenticatedSessionAsync(CancellationToken token)
        {
            try
            {
                var cfg = _config!.DashboardSYT;
                var baseUrl = cfg.BaseUrl.TrimEnd('/');
                var loginUrl = $"{baseUrl}/api/grant/login";

                LogInfo("Đăng nhập API...");
                var loginPayload = JsonSerializer.Serialize(new { username = cfg.Auth.Username, password = cfg.Auth.Password });
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var loginResp = await client.PostAsync(loginUrl, new StringContent(loginPayload, Encoding.UTF8, "application/json"), token);
                var loginJson = await loginResp.Content.ReadAsStringAsync(token);

                if (!loginResp.IsSuccessStatusCode)
                {
                    LogError($"Đăng nhập thất bại: {loginJson}");
                    return null;
                }

                var accessToken = JsonDocument.Parse(loginJson).RootElement.GetProperty("access_token").GetString()!;
                LogSuccess("Đăng nhập API thành công.");

                LogInfo("Lấy APP_ACCESS_KEY...");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("authorization", accessToken);
                var keyResp = await client.GetAsync($"{baseUrl}/bireport/v1/get_api_access_key", token);
                var keyJson = await keyResp.Content.ReadAsStringAsync(token);

                if (!keyResp.IsSuccessStatusCode)
                {
                    LogError($"Lỗi lấy APP_ACCESS_KEY: {keyJson}");
                    return null;
                }

                var appAccessKey = JsonDocument.Parse(keyJson).RootElement.GetProperty("token").GetString()!;
                LogSuccess("Lấy APP_ACCESS_KEY thành công.");
                return new AuthSession(appAccessKey);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                LogError($"Lỗi xác thực: {ex.Message}");
                return null;
            }
        }

        private async Task ProcessSqlFileAsync(SqlFileInfo fileInfo, string appAccessKey, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                var jsonBody = await ExecuteSqlAndGetJsonAsync(fileInfo.FullPath, token);
                await SendToApiAsync(fileInfo.Endpoint, jsonBody, appAccessKey, token);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                LogError($"Lỗi xử lý {fileInfo.Name}: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> ExecuteSqlAndGetJsonAsync(string sqlPath, CancellationToken token)
        {
            var sql = await File.ReadAllTextAsync(sqlPath, Encoding.UTF8, token);
            var connStr = DatabaseService.GetConnectionString(_config!.DashboardSYT.DatabaseSource);

            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(token);
            await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 300 };
            await using var reader = await cmd.ExecuteReaderAsync(token);

            return await reader.ReadAsync(token)
                ? NormalizeJson(reader.GetString(0))
                : "{\"DANHSACH\":[]}";
        }

        private static string NormalizeJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var result = new Dictionary<string, JsonElement>();

                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "DANHSACH", StringComparison.OrdinalIgnoreCase))
                    {
                        result[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                            ? JsonDocument.Parse("[]").RootElement
                            : prop.Value.Clone();
                    }
                    else
                    {
                        result[prop.Name] = prop.Value.Clone();
                    }
                }
                return JsonSerializer.Serialize(result, JsonOptions);
            }
            catch
            {
                return "{\"DANHSACH\":[]}";
            }
        }

        private async Task SendToApiAsync(string endpoint, string jsonBody, string appAccessKey, CancellationToken token)
        {
            var baseUrl = _config!.DashboardSYT.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/{endpoint}";

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("X-APP-ACCESS-KEY", appAccessKey);
                    var resp = await client.PostAsync(url, new StringContent(jsonBody, Encoding.UTF8, "application/json"), token);
                    var result = await resp.Content.ReadAsStringAsync(token);

                    var shortResult = result;
                    try
                    {
                        using var doc = JsonDocument.Parse(result);
                        var dict = new Dictionary<string, object>();
                        foreach (var p in doc.RootElement.EnumerateObject())
                            if (!string.Equals(p.Name, "DANHSACH", StringComparison.OrdinalIgnoreCase))
                                dict[p.Name] = p.Value.Clone();
                        shortResult = JsonSerializer.Serialize(dict, JsonOptions);
                    }
                    catch { }

                    if (resp.IsSuccessStatusCode)
                    {
                        LogSuccess($"Gửi [{endpoint}] thành công: {shortResult}");
                        return;
                    }

                    LogError($"Gửi [{endpoint}] lỗi ({(int)resp.StatusCode}): {shortResult}");
                    if (i == 2) return;
                    await Task.Delay(TimeSpan.FromSeconds(1 << i), token);
                }
                catch (Exception ex) when (i < 2 && !(ex is OperationCanceledException))
                {
                    LogWarning($"Retry {i + 1} cho [{endpoint}]: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(1 << i), token);
                }
            }
        }

        public void SaveConfig(DashboardSYTConfig syt)
        {
            try
            {
                var appConfig = ConfigService.Load();
                appConfig.DashboardSYT = syt;
                ConfigService.Save(appConfig);
                LogSuccess("Đã lưu cấu hình Dashboard SYT.");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khi lưu cấu hình: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _semaphore.Dispose();
            client.Dispose();
        }
    }
}