using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppSecretRotationFunc
{
    public static class RotateAppSecretsOrchestrator
    {
        [FunctionName("RotateAppSecretsOrchestrator")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var appIds = context.GetInput<List<string>>();

            // List to store new secrets for Key Vault update
            var appSecretDataList = new List<AppSecretData>();

            // Rotate secrets for each app
            foreach (var appId in appIds)
            {
                // Rotate the secret and get the new secret data
                var newSecretData = await context.CallActivityAsync<AppSecretData>("RotateAppSecret", appId);
                appSecretDataList.Add(newSecretData);
                if (bool.TryParse(Environment.GetEnvironmentVariable("DeleteOldSecrests"), out bool isFeatureEnabled) && isFeatureEnabled)
                {
                    // Execute your logic here when the environment variable is 'true'
                    await context.CallActivityAsync("RmAppOldSecret", appId);
                }
            }

            // Update Key Vault with the new secrets
            foreach (var appSecretData in appSecretDataList)
            {
                await context.CallActivityAsync("UpdateKeyVault", appSecretData);
            }
        }
    }
}
