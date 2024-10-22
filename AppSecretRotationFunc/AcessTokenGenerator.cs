using Azure.Core;
using Azure.Identity;
using System.Threading.Tasks;

namespace AppSecretRotationFunc
{
    public static class AcessTokenGenerator
    {
        public static async Task<string> GetAccessToken()
        {
            // Resource you want to authenticate with, such as the Microsoft Graph API
            string[] scopes = new[] { "https://graph.microsoft.com/.default" };

            // Create a DefaultAzureCredential to automatically use Managed Identity or other Azure identity mechanisms
            var credential = new DefaultAzureCredential();

            // Obtain an access token for the resource
            AccessToken token = await credential.GetTokenAsync(new TokenRequestContext(scopes));

            // return
            return token.Token;
        }
    }
}
