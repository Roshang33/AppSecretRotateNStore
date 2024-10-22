using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AppSecretRotationFunc
{
    public static class RotateAppSecretsStarter
    {
        [FunctionName("RotateAppSecretsStarter")]
        public static async Task Run(
            [TimerTrigger("0 0 */12 * * *")] TimerInfo myTimer,  // Runs daily at midnight
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"RotateAppSecretsStarter function executed at: {DateTime.Now}");

            // Create confidential client application and graph client


            try
            {
                string credential = await AcessTokenGenerator.GetAccessToken();
                // Get all app registrations
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);

                var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/applications");
                var content = await response.Content.ReadAsStringAsync();
                dynamic spnData = JsonConvert.DeserializeObject(content);
                var appRegistrationIds = new List<string>();
                foreach (var sp in spnData.value)
                {
                    Console.WriteLine((string)sp.id);
                    appRegistrationIds.Add((string)sp.id);
                }

                // Start the orchestrator with the list of app IDs
                await starter.StartNewAsync("RotateAppSecretsOrchestrator", appRegistrationIds);
            }
            catch (Exception ex)
            {
                log.LogError($"Error starting orchestrator: {ex.Message}");
            }
        }


    }
}
