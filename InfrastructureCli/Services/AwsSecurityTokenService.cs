using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System.Threading.Tasks;

namespace InfrastructureCli.Services
{
    public static class AwsSecurityTokenService
    {
        private static readonly IAmazonSecurityTokenService Client = new AmazonSecurityTokenServiceClient();

        public static async Task<string> GetAccountId()
        {
            var request = new GetCallerIdentityRequest();

            var response = await Client.GetCallerIdentityAsync(request);

            return response.Account;
        }
    }
}
