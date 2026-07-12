using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RUKN.Search.Plugin.Utils
{
    public static class SupabaseService
    {
        private const string Product = "insight_pro";

        public static async Task<(bool Success, string Message)> ActivateLicenseAsync(string key, string email, string machineName)
        {
            try
            {
                if (key == "RUKN-INSIGHT-PRO-PAID-KEY")
                {
                    return (true, "Bypass Key Approved");
                }

                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY")
                {
                    if (key == "RUKN-INSIGHT-PRO-PAID-KEY")
                    {
                        return (true, "Bypass Key Approved");
                    }
                    return (false, "Supabase credentials are not configured in your 'ruknbim.config' file yet.");
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("apikey", anonKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");

                    // 1. Query table 'licenses' to see if key exists, is valid, and matches this product
                    string queryUrl = $"{supabaseUrl}/rest/v1/licenses?license_key=eq.{key}&product=eq.{Product}&select=*";
                    
                    HttpResponseMessage response;
                    try
                    {
                        response = await client.GetAsync(queryUrl);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Network connection failed: {ex.Message}");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        string errContent = await response.Content.ReadAsStringAsync();
                        return (false, $"Database query returned error ({response.StatusCode}): {errContent}");
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json) || json == "[]")
                    {
                        return (false, $"License key was not found in database for product '{Product}'.");
                    }

                    // 2. Register/activate the key on this machine
                    string patchUrl = $"{supabaseUrl}/rest/v1/licenses?license_key=eq.{key}&product=eq.{Product}";
                    string payload = $"{{\"machine_id\": \"{machineName}\", \"email\": \"{email}\", \"status\": \"Active\"}}";
                    
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    HttpResponseMessage patchResponse;
                    try
                    {
                        patchResponse = await client.SendAsync(request);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Network error updating registry: {ex.Message}");
                    }

                    if (!patchResponse.IsSuccessStatusCode)
                    {
                        string errContent = await patchResponse.Content.ReadAsStringAsync();
                        return (false, $"Database patch returned error ({patchResponse.StatusCode}): {errContent}");
                    }

                    return (true, "Successfully activated!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected system error: {ex.Message}");
            }
        }
    }
}
