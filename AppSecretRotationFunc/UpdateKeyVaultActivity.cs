using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AppSecretRotationFunc
{
    public static class UpdateKeyVaultActivity
    {
        [FunctionName("UpdateKeyVault")]
        public static async Task UpdateKeyVault(
            [ActivityTrigger] AppSecretData appSecretData,
            ILogger log)
        {
            // Load Key Vault URL from environment variables
            
            string keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultUrl");
            

            // Create the SecretClient to interact with Key Vault
            //var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            try
            {
                

                //var credential = new StaticTokenCredential(accessToken);
                var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                KeyVaultSecret secret = new KeyVaultSecret(appSecretData.AppDisplayName, appSecretData.SecretValue)
                {
                    Properties =
                                        {
                                            ExpiresOn = appSecretData.SecretExpiryDate
                                        }
                };
                await client.SetSecretAsync(secret);
            }
            catch (Exception ex)
            {
                log.LogError($"Error storing secret for app {appSecretData.AppDisplayName} in Key Vault: {ex.Message}");
                throw;
            }
        }
    }
}
