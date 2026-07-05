using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ApplicationParts;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RUKN.Search.Plugin
{
    public class Analytics
    {
        private static string apiUrl = "https://expoterAPI";
        private static HttpClient client = new HttpClient();
        public static async Task Send(string action, string details)
        {
            DateTime currentDate = DateTime.Now;
            string date = currentDate.ToString();

            RegionInfo currentRegion = new RegionInfo(CultureInfo.CurrentCulture.Name);
            string location = currentRegion.EnglishName.ToString();

            string user = Environment.UserName;
            ApplicationVersion versionNavis = Application.Version;
            string software = string.Concat("Navisworks ", versionNavis.ApiMajor);
            string version = SettingsConfig.currentVersion;

            // Use string interpolation to incorporate variables into the JSON string
            string json = $@"{{
                ""User"": ""{user}"",
                ""Location"": ""{location}"",
                ""Software"": ""{software}"",
                ""Date"": ""{date}"",
                ""Action"": ""{action}"",
                ""Details"": ""{details}"",
                ""Version"": ""{version}""
            }}";

            try
            {

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string apiKey = SettingsConfig.GetValue("apikey"); ;
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Request successful");
                }
                else
                {
                    Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}


