using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RUKN.Search.Plugin.Utils
{
    public static class SupabaseLicensing
    {
        private const string Product = "rukn_insight_pro";

        private static readonly string _logFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RUKNBIM", "license_log.txt");

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static async Task<bool> ActivateLicenseAsync(string key, string email, string machineName)
        {
            try
            {
                // Navisworks hosts .NET Framework; make sure TLS 1.2 is enabled for Supabase
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY")
                {
                    Log($"Activate: Supabase not configured (url={(supabaseUrl ?? "null")}, anonKey={(string.IsNullOrEmpty(anonKey) ? "missing" : "placeholder")}). Using offline fallback.");
                    // Fallback to local offline validation if Supabase is not configured yet
                    return key == "RUKN-INSIGHT-PRO-PAID-KEY";
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("apikey", anonKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");

                    // 1. Query table 'licenses' for a matching, active, unexpired key for this product
                    string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    string encodedKey = Uri.EscapeDataString(key);
                    string queryUrl = $"{supabaseUrl}/rest/v1/licenses" +
                        $"?license_key=eq.{encodedKey}" +
                        $"&product=eq.{Product}" +
                        $"&status=eq.Active" +
                        $"&expiry_date=gte.{today}" +
                        $"&select=*";
                    var response = await client.GetAsync(queryUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        Log($"Activate: GET failed with HTTP {(int)response.StatusCode} for key '{key}'.");
                        return false;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json) || json == "[]")
                    {
                        Log($"Activate: key '{key}' not found / inactive / expired (empty result).");
                        return false; // Key not found, inactive, or expired
                    }

                    // 2. Register/activate the key on this machine
                    string patchUrl = $"{supabaseUrl}/rest/v1/licenses?license_key=eq.{encodedKey}&product=eq.{Product}";
                    string payload = $"{{\"machine_id\": \"{EscapeJson(machineName)}\", \"email\": \"{EscapeJson(email)}\"}}";

                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    var patchResponse = await client.SendAsync(request);
                    Log($"Activate: key '{key}' valid; PATCH machine registration returned HTTP {(int)patchResponse.StatusCode}.");
                    return patchResponse.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Log($"Activate: exception - {ex.GetType().Name}: {ex.Message}" +
                    (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : ""));
                // Fallback to offline verification during network failures
                return key == "RUKN-INSIGHT-PRO-PAID-KEY";
            }
        }

        /// <summary>
        /// Best-effort: records a started trial as a row in the shared 'licenses' table so it's visible in Supabase.
        /// The local 14-day trial clock (SettingsConfig.TrialStartDate) remains the source of truth for the app,
        /// so failures here are swallowed and never block the trial from starting.
        /// </summary>
        public static async Task RegisterTrialAsync(string email, string machineName)
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY")
                {
                    Log("Trial: Supabase not configured; trial not mirrored online.");
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("apikey", anonKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");

                    string expiryDate = DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-dd");
                    string postUrl = $"{supabaseUrl}/rest/v1/licenses";
                    string payload = "{" +
                        $"\"email\": \"{EscapeJson(email)}\", " +
                        $"\"machine_id\": \"{EscapeJson(machineName)}\", " +
                        "\"license_type\": \"Trial\", " +
                        "\"status\": \"Active\", " +
                        $"\"expiry_date\": \"{expiryDate}\", " +
                        $"\"product\": \"{Product}\"" +
                        "}";

                    var request = new HttpRequestMessage(HttpMethod.Post, postUrl)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    var postResponse = await client.SendAsync(request);
                    Log($"Trial: POST for '{email}' returned HTTP {(int)postResponse.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                Log($"Trial: exception - {ex.GetType().Name}: {ex.Message}" +
                    (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : ""));
                // Best-effort only: local trial already works offline
            }
        }

        private static string EscapeJson(string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
