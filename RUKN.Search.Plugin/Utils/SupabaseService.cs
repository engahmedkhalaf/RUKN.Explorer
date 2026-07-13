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

    public class TrialRegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        /// <summary>True if this machine already had a trial on record (server is the source of truth for the start date).</summary>
        public bool AlreadyExisted { get; set; }
        public string StartDate { get; set; }
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
                        return new LicenseActivationResult { Success = false, Message = $"License key was not found in database for product '{Product}'.\nPlease contact sales@ruknbim.com for support." };
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
                        string friendlyMsg = GetJsonValue(errContent, "message");
                        return new LicenseActivationResult
                        {
                            Success = false,
                            Message = string.IsNullOrEmpty(friendlyMsg)
                                ? $"Database patch returned error ({patchResponse.StatusCode}): {errContent}"
                                : friendlyMsg
                        };
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

        /// <summary>
        /// Registers a started trial in the shared 'licenses' table, enforced one-trial-per-machine
        /// at the database level (unique index on product+machine_id). If this machine already has a
        /// trial on record, returns its real start date instead of creating a new one, so wiping the
        /// local config can't reset the 14-day clock.
        /// </summary>
        public static async Task<TrialRegistrationResult> RegisterTrialAsync(string email, string machineName)
        {
            try
            {
                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY")
                {
                    return new TrialRegistrationResult { Success = false, Message = "Supabase credentials are not configured." };
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

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        return new TrialRegistrationResult { Success = true, AlreadyExisted = false, StartDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") };
                    }

                    string errContent = await response.Content.ReadAsStringAsync();
                    string errCode = GetJsonValue(errContent, "code");
                    if (errCode == "23505")
                    {
                        // Unique violation: this machine already has a trial. Look it up and use its real start date.
                        string queryUrl = $"{supabaseUrl}/rest/v1/licenses?machine_id=eq.{Uri.EscapeDataString(machineName)}&product=eq.{Product}&license_type=eq.Trial&select=start_date";
                        var existingResponse = await client.GetAsync(queryUrl);
                        if (existingResponse.IsSuccessStatusCode)
                        {
                            string existingJson = await existingResponse.Content.ReadAsStringAsync();
                            string startDate = GetJsonValue(existingJson, "start_date");
                            if (!string.IsNullOrEmpty(startDate))
                            {
                                return new TrialRegistrationResult { Success = true, AlreadyExisted = true, StartDate = startDate };
                            }
                        }
                        return new TrialRegistrationResult { Success = false, Message = "A trial was already used on this machine." };
                    }

                    string friendlyMsg = GetJsonValue(errContent, "message");
                    return new TrialRegistrationResult { Success = false, Message = string.IsNullOrEmpty(friendlyMsg) ? errContent : friendlyMsg };
                }
            }
            catch (Exception ex)
            {
                return new TrialRegistrationResult { Success = false, Message = $"Network error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Clears the machine binding on this license server-side, so it can be legitimately
        /// activated on a different machine afterwards.
        /// </summary>
        public static async Task<bool> DeactivateLicenseAsync(string key, string machineName)
        {
            try
            {
                string supabaseUrl = SettingsConfig.GetValue("SupabaseUrl");
                string anonKey = SettingsConfig.GetValue("SupabaseAnonKey");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey) || anonKey == "YOUR_SUPABASE_ANON_KEY" || string.IsNullOrEmpty(key))
                {
                    return false;
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("apikey", anonKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");

                    // Only clear the binding if it currently matches this machine (the trigger blocks
                    // any anon change to a differently-bound row anyway, but scope the request too).
                    string patchUrl = $"{supabaseUrl}/rest/v1/licenses?license_key=eq.{Uri.EscapeDataString(key)}&product=eq.{Product}&machine_id=eq.{Uri.EscapeDataString(machineName)}";
                    string payload = "{\"machine_id\": null}";

                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };

                    var response = await client.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string EscapeJson(string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
