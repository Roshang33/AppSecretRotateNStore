using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppSecretRotationFunc
{
    public static class RotateAppSecretActivity
    {
        [FunctionName("RotateAppSecret")]
        public static async Task<AppSecretData> RotateAppSecret(
            [ActivityTrigger] string appId,
            ILogger log)
        {
            // Load tenant ID, client ID, and client secret from environment variables


            try
            {
                string credential = await AcessTokenGenerator.GetAccessToken();

                // Get the app registration
                DateTime ExpiryDate = DateTime.UtcNow.AddMonths(3);

                var secretPayload = $@"
            {{
                   ""passwordCredential"": {{
                        ""displayName"": ""New Client Secret"",
                        ""startDateTime"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
                        ""endDateTime"": ""{DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-ddTHH:mm:ssZ")}""
        
                                            }}
            }}";

                var content = new StringContent(secretPayload, Encoding.UTF8, "application/json");
                var client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);

                var response = await client.PostAsync($"https://graph.microsoft.com/beta/applications/{appId}/addPassword", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Token request failed with status code {response.StatusCode}: {errorContent}");
                    throw new Exception($"Failed to obtain token: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonDocument.Parse(responseContent).RootElement;

                string AppDisplayName = await GetAppDisplayName(appId, credential);

                // Return new secret data for Key Vault update
                return new AppSecretData
                {
                    AppId = appId,
                    AppDisplayName = AppDisplayName,
                    SecretValue = responseData.GetProperty("secretText").GetString(),
                    SecretExpiryDate = ExpiryDate,
                };
            }
            catch (Exception ex)
            {
                log.LogError($"Error rotating secret for app {appId}: {ex.Message}");
                throw;
            }
        }

        public static async Task<string> GetAppDisplayName(string appId, string credential)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);

                // Call the Microsoft Graph API to get the app registration details
                var url = $"https://graph.microsoft.com/v1.0/applications/{appId}";
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                // Extract and return the display name
                return json["displayName"]?.ToString();
            }

        }
    }

    public class AppSecretData
    {
        public string AppId { get; set; }
        public string AppDisplayName { get; set; }
        public string SecretValue { get; set; }

        public DateTime SecretExpiryDate { get; set; }
    }
}
