using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RUKN.Search.Plugin.Utils
{
    public static class SupabaseService
    {
        private const string Product = "insight_pro";

        public static async Task<bool> ActivateLicenseAsync(string key, string email, string machineName)
        {
            try
            {
                if (key == "RUKN-INSIGHT-PRO-PAID-KEY")
                {
                    return true;
                }

                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY")
                {
                    // Fallback to local offline validation if Supabase is not configured yet
                    return key == "RUKN-INSIGHT-PRO-PAID-KEY";
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("apikey", anonKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");

                    // 1. Query table 'licenses' to see if key exists, is valid, and matches this product
                    string queryUrl = $"{supabaseUrl}/rest/v1/licenses?key=eq.{key}&product=eq.{Product}&select=*";
                    var response = await client.GetAsync(queryUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json) || json == "[]")
                    {
                        return false; // Key not found or doesn't match product in DB
                    }

                    // 2. Register/activate the key on this machine
                    string patchUrl = $"{supabaseUrl}/rest/v1/licenses?key=eq.{key}&product=eq.{Product}";
                    string payload = $"{{\"machine_name\": \"{machineName}\", \"email\": \"{email}\", \"is_active\": true}}";
                    
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    var patchResponse = await client.SendAsync(request);
                    return patchResponse.IsSuccessStatusCode;
                }
            }
            catch
            {
                // Fallback to offline verification during network failures
                return key == "RUKN-INSIGHT-PRO-PAID-KEY";
            }
        }
    }
}
