using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using _3XSyncHub.Models;
using System.Reflection;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.ConfigService.cs
    // 🔖 Version: 20251120_0755
    // 📌 Nội dung đã xử lý:
    // - Đọc / ghi AppConfig từ Config/Config.json
    // - Tự tạo file config mặc định nếu chưa tồn tại
    // - Hỗ trợ enum dạng string bằng JsonStringEnumConverter
    // - Dùng JsonSerializerOptions thống nhất toàn hệ thống
    // - Thêm mã hóa/giải mã Password bằng reflection (mức 1 – key cố định)
    // ============================================================================

    public static class ConfigService
    {
        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Config.json");

        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // ✔️ AES-256 key cố định (mức 1)
        private const string Base64Key = "u2Qf1yV4p8Zs9x3bT6rN5w0aQe7mH2cJYkL0vPqR8sU=";
        private static readonly byte[] Key = Convert.FromBase64String(Base64Key);

        // ============================================================================
        //  Tạo file config mặc định
        // ============================================================================
        public static void EnsureConfigExists()
        {
            try
            {
                string configDir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                if (!File.Exists(ConfigPath))
                {
                    var defaultConfig = new AppConfig();
                    EncryptAllPasswords(defaultConfig); // Mã hóa pass trước khi tạo file
                    string json = JsonSerializer.Serialize(defaultConfig, _options);
                    File.WriteAllText(ConfigPath, json, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo file cấu hình mặc định: {ex.Message}", ex);
            }
        }

        // ============================================================================
        //  Đọc cấu hình + giải mã Password*
        // ============================================================================
        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    throw new FileNotFoundException($"Không tìm thấy file cấu hình: {ConfigPath}");

                string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AppConfig>(json, _options) ?? new AppConfig();

                // ⭐ Giải mã tất cả Password*
                DecryptAllPasswords(config);

                return config;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi đọc cấu hình: {ex.Message}", ex);
            }
        }

        // ============================================================================
        //  Ghi cấu hình + mã hóa Password*
        // ============================================================================
        public static void Save(AppConfig config)
        {
            try
            {
                // ⭐ Mã hóa tất cả Password* trước khi lưu
                EncryptAllPasswords(config);

                string json = JsonSerializer.Serialize(config, _options);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi ghi cấu hình: {ex.Message}", ex);
            }
        }

        // ============================================================================
        // Reflection: mã hóa TẤT CẢ property Password / Pass / ... (đệ quy)
        // ============================================================================
        private static void EncryptAllPasswords(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            if (type.IsPrimitive || obj is string) return;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                var value = prop.GetValue(obj);

                // Password hoặc chứa Pass
                if (prop.PropertyType == typeof(string) &&
                    (prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                     prop.Name.Contains("Pass", StringComparison.OrdinalIgnoreCase)))
                {
                    var pwd = value as string;
                    if (!string.IsNullOrEmpty(pwd) && !pwd.StartsWith("ENC:"))
                    {
                        prop.SetValue(obj, "ENC:" + EncryptString(pwd));
                    }
                }
                else if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                {
                    EncryptAllPasswords(value);
                }
            }
        }

        private static void DecryptAllPasswords(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            if (type.IsPrimitive || obj is string) return;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                var value = prop.GetValue(obj);

                if (prop.PropertyType == typeof(string) &&
                    (prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                     prop.Name.Contains("Pass", StringComparison.OrdinalIgnoreCase)))
                {
                    var pwd = value as string;
                    if (!string.IsNullOrEmpty(pwd) && pwd.StartsWith("ENC:"))
                    {
                        try
                        {
                            prop.SetValue(obj, DecryptString(pwd[4..]));
                        }
                        catch
                        {
                            prop.SetValue(obj, string.Empty);
                        }
                    }
                }
                else if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                {
                    DecryptAllPasswords(value);
                }
            }
        }

        // ============================================================================
        // AES Encrypt / Decrypt
        // ============================================================================
        private static string EncryptString(string plainText)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var enc = aes.CreateEncryptor(aes.Key, iv);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        private static string DecryptString(string cipherBase64)
        {
            var full = Convert.FromBase64String(cipherBase64);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Key;

            var ivLen = aes.BlockSize / 8;
            var iv = new byte[ivLen];
            var cipher = new byte[full.Length - ivLen];

            Buffer.BlockCopy(full, 0, iv, 0, ivLen);
            Buffer.BlockCopy(full, ivLen, cipher, 0, cipher.Length);

            using var dec = aes.CreateDecryptor(aes.Key, iv);
            var plainBytes = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
