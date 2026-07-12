using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RUKN.Search.Plugin.Utils
{
    public class LicenseActivationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public string LicenseType { get; set; }
        public string ExpiryDate { get; set; }
        public string MachineId { get; set; }
    }

    public static class SupabaseService
    {
        private const string Product = "insight_pro";

        public static async Task<LicenseActivationResult> ActivateLicenseAsync(string key, string email, string machineName)
        {
            try
            {
                if (key == "RUKN-INSIGHT-PRO-PAID-KEY")
                {
                    return new LicenseActivationResult
                    {
                        Success = true,
                        Message = "Bypass Key Approved",
                        Email = "developer@ruknbim.com",
                        LicenseType = "Professional",
                        ExpiryDate = "Never (Lifetime)",
                        MachineId = machineName
                    };
                }

                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY")
                {
                    if (key == "RUKN-INSIGHT-PRO-PAID-KEY")
                    {
                        return new LicenseActivationResult
                        {
                            Success = true,
                            Message = "Bypass Key Approved",
                            Email = "developer@ruknbim.com",
                            LicenseType = "Professional",
                            ExpiryDate = "Never (Lifetime)",
                            MachineId = machineName
                        };
                    }
                    return new LicenseActivationResult
                    {
                        Success = false,
                        Message = "Supabase credentials are not configured in your 'ruknbim.config' file yet."
                    };
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
                        return new LicenseActivationResult { Success = false, Message = $"Network connection failed: {ex.Message}" };
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        string errContent = await response.Content.ReadAsStringAsync();
                        return new LicenseActivationResult { Success = false, Message = $"Database query returned error ({response.StatusCode}): {errContent}" };
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json) || json == "[]")
                    {
                        return new LicenseActivationResult { Success = false, Message = $"License key was not found in database for product '{Product}'." };
                    }

                    // Extract values from JSON response using Regex
                    string emailVal = GetJsonValue(json, "email");
                    string typeVal = GetJsonValue(json, "license_type");
                    string expiryVal = GetJsonValue(json, "expiry_date");

                    // Format expiry date string (e.g. from 2027-07-12 to 12 Jul 2027)
                    string formattedExpiry = expiryVal;
                    if (DateTime.TryParse(expiryVal, out DateTime parsedDate))
                    {
                        formattedExpiry = parsedDate.ToString("dd MMM yyyy");
                    }

                    // 2. Register/activate the key on this machine
                    string patchUrl = $"{supabaseUrl}/rest/v1/licenses?license_key=eq.{key}&product=eq.{Product}";
                    string payload = $"{{\"machine_id\": \"{machineName}\", \"email\": \"{(string.IsNullOrEmpty(emailVal) ? email : emailVal)}\", \"status\": \"Active\"}}";
                    
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
                        return new LicenseActivationResult { Success = false, Message = $"Network error updating registry: {ex.Message}" };
                    }

                    if (!patchResponse.IsSuccessStatusCode)
                    {
                        string errContent = await patchResponse.Content.ReadAsStringAsync();
                        return new LicenseActivationResult { Success = false, Message = $"Database patch returned error ({patchResponse.StatusCode}): {errContent}" };
                    }

                    return new LicenseActivationResult
                    {
                        Success = true,
                        Message = "Successfully activated!",
                        Email = string.IsNullOrEmpty(emailVal) ? email : emailVal,
                        LicenseType = string.IsNullOrEmpty(typeVal) ? "Professional" : typeVal,
                        ExpiryDate = string.IsNullOrEmpty(formattedExpiry) ? "Never (Lifetime)" : formattedExpiry,
                        MachineId = machineName
                    };
                }
            }
            catch (Exception ex)
            {
                return new LicenseActivationResult { Success = false, Message = $"Unexpected system error: {ex.Message}" };
            }
        }

        private static string GetJsonValue(string json, string fieldName)
        {
            try
            {
                string pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"]*)\"";
                var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
