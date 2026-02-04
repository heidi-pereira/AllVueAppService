using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace DashboardBuilder
{
    public class MonitorHelper
    {
        public const string SETTING_KEY_URL_SUCCESS = "MonitorPingUrlSuccess";
        public const string SETTING_KEY_URL_FAIL = "MonitorPingUrlFail";
        private class CreateSnitchResponse
        {
            public string error { get; set; }
            public string name { get; set; }
            public string check_in_url { get; set; }
        }

        public static void RegisterSnitch(string apiKey, string shortCode)
        {
            using (var client = new HttpClient())
            {
                var base64AuthorizationString = $"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey))}:";
                var content = new StringContent(JsonConvert.SerializeObject(new
                {
                    name = $"DashboardBuild { shortCode }",
                    tags = new string[] { "DashboardBuild" },
                    interval = "daily"
                }));
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64AuthorizationString);
                var httpResponse = client.PostAsync("https://api.deadmanssnitch.com/v1/snitches", content).Result;
                var response = JsonConvert.DeserializeObject<CreateSnitchResponse>(httpResponse.Content.ReadAsStringAsync().Result);

                if (string.IsNullOrEmpty(response.error))
                {
                    Console.WriteLine($"{shortCode} Results:");
                    var uriBuilder = new UriBuilder(response.check_in_url);
                    var failQueryString = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    failQueryString["s"] = "1";
                    var failUriBuilder = new UriBuilder(response.check_in_url)
                    {
                        Query = failQueryString.ToString()
                    };
                    var output = $"{SETTING_KEY_URL_SUCCESS}\t{uriBuilder}\n{SETTING_KEY_URL_FAIL}\t{failUriBuilder}";
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine($"{shortCode}\nError: {response.error}");
                }
            }
        }
    }
}
