using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using _3XSyncHub.Models;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.SyncService.cs
    // 🔖 Version: 20251119_1148
    // 📌 Nội dung đã xử lý:
    // - Đồng bộ đa kênh song song (mỗi channel có SQL + endpoint riêng)
    // - Thực thi SQL → lấy JSON → POST lên API ngoài
    // - Header động theo từng channel (AuthHeader + AccessKey)
    // - Log chuẩn LogService cho từng channel (Info / Success / Warning / Error)
    // ============================================================================

    public class SyncService
    {
        private readonly HttpClient client = new();

        /// Đồng bộ toàn bộ các channel song song (SYTPTO, DuocQG, EMR, HIS, ...)
        public async Task<bool> SendAllChannelsAsync(SyncConfig config)
        {
            if (config.Channels == null || config.Channels.Count == 0)
            {
                LogService.Warning("SyncService", "Không có channel nào trong cấu hình.");
                return false;
            }

            LogService.Info("SyncService", $"Bắt đầu đồng bộ {config.Channels.Count} channel song song...");

            var tasks = config.Channels
                              .Select(channel => Task.Run(() => SendChannelAsync(channel)))
                              .ToList();

            await Task.WhenAll(tasks);

            LogService.Success("SyncService", "Đã hoàn tất đồng bộ tất cả channel.");
            return true;
        }

        /// Xử lý đồng bộ riêng từng channel (độc lập)
        private async Task SendChannelAsync(SyncChannel channel)
        {
            try
            {
                string sqlFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, channel.SqlFolder);
                if (!Directory.Exists(sqlFolder))
                {
                    LogService.Warning("SyncService", $"[{channel.Name}] Không tìm thấy thư mục SQL: {sqlFolder}");
                    return;
                }

                var sqlFiles = Directory.GetFiles(sqlFolder, "*.sql");
                if (sqlFiles.Length == 0)
                {
                    LogService.Warning("SyncService", $"[{channel.Name}] Không có file SQL nào trong {sqlFolder}");
                    return;
                }

                LogService.Info("SyncService", $"[{channel.Name}] Bắt đầu đồng bộ {sqlFiles.Length} file SQL...");

                foreach (var file in sqlFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string sql = await File.ReadAllTextAsync(file, Encoding.UTF8);

                    try
                    {
                        var result = await DatabaseService.ExecuteScalarAsync(sql);
                        string jsonData = result?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(jsonData))
                        {
                            LogService.Warning("SyncService", $"[{channel.Name}] Không có dữ liệu cho [{fileName}], bỏ qua.");
                            continue;
                        }

                        string url = $"{channel.BaseUrl.TrimEnd('/')}/{channel.ApiPrefix.TrimStart('/')}/{fileName}";
                        var request = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
                        };

                        // Header động theo từng channel
                        if (!string.IsNullOrWhiteSpace(channel.AuthHeader) &&
                            !string.IsNullOrWhiteSpace(channel.AccessKey))
                        {
                            request.Headers.Add(channel.AuthHeader, channel.AccessKey);
                        }

                        var response = await client.SendAsync(request);
                        string body = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                            LogService.Success("SyncService", $"[{channel.Name}] Gửi [{fileName}] thành công ({response.StatusCode}).");
                        else
                            LogService.Error("SyncService", $"[{channel.Name}] Lỗi API ({(int)response.StatusCode}): {body}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("SyncService", $"[{channel.Name}] Lỗi xử lý file [{fileName}]: {ex.Message}");
                    }
                }

                LogService.Success("SyncService", $"[{channel.Name}] Hoàn tất đồng bộ {sqlFiles.Length} file.");
            }
            catch (Exception ex)
            {
                LogService.Error("SyncService", $"[{channel.Name}] Lỗi tổng: {ex.Message}");
            }
        }

        /// Hàm GET API (chuẩn hóa, có thể dùng cho các channel trong tương lai)
        public async Task<string> GetDataAsync(string baseUrl, string endpoint, string headerKey,
                                               string accessKey, string channelName)
        {
            try
            {
                string url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrWhiteSpace(headerKey) &&
                    !string.IsNullOrWhiteSpace(accessKey))
                {
                    request.Headers.Add(headerKey, accessKey);
                }

                var response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    LogService.Success("SyncService", $"[{channelName}] GET [{endpoint}] thành công.");
                    return body;
                }

                LogService.Error("SyncService", $"[{channelName}] GET [{endpoint}] lỗi ({(int)response.StatusCode}): {body}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogService.Error("SyncService", $"[{channelName}] Lỗi GET [{endpoint}]: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
