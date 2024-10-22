using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppSecretRotationFunc
{
    public static class RmAppOldSecretAcitvity
    {
        [FunctionName("RmAppOldSecret")]
        public static async Task RmAppOldSecret(
            [ActivityTrigger] string appId,
            ILogger log)
        {
            // Load tenant ID, client ID, and client secret from environment variables


            try
            {
                string credential = await AcessTokenGenerator.GetAccessToken();
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);

                var response = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/applications/{appId}?$select=passwordCredentials");

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(responseBody);

                var secrets = json.RootElement.GetProperty("passwordCredentials")
                    .EnumerateArray()
                    .Select(element => new PasswordCredential
                    {
                        keyId = element.GetProperty("keyId").GetString(),
                        endDateTime = element.GetProperty("endDateTime").GetDateTime()
                    })
                    .ToList();

                if (secrets == null || secrets.Count == 0)
                {
                    Console.WriteLine("No secrets found for this app registration.");
                    return;
                }


                // Step 2: Sort the secrets and identify the latest one
                var latestSecret = secrets.OrderByDescending(s => s.endDateTime).FirstOrDefault();
                var secretsToDelete = secrets.Where(s => s.keyId != latestSecret.keyId).ToList();

                if (secretsToDelete.Count == 0)
                {
                    Console.WriteLine("There are no other secrets to delete.");
                    return;
                }
                // Step 3: Delete all secrets except the latest one
                foreach (var secret in secretsToDelete)
                {
                    await DeleteSecret(httpClient, appId, secret.keyId);
                    Console.WriteLine($"Deleted secret with KeyId: {secret.keyId}");
                }

            }
            catch (Exception ex)
            {
                log.LogError($"Error rotating secret for app {appId}: {ex.Message}");
                throw;
            }
        }

        private static async Task DeleteSecret(HttpClient httpClient, string appRegistrationId, string keyId)
        {
            var url = $"https://graph.microsoft.com/v1.0/applications/{appRegistrationId}/removePassword";
            var content = new StringContent(JsonSerializer.Serialize(new { keyId }), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }


    }

    public class PasswordCredential
    {
        public string keyId { get; set; }
        public DateTime endDateTime { get; set; }
    }
}
