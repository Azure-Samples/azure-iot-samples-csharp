using Azure;
using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Service;
using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningAzureSasCredentialSample
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> parserResult = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            // Initialize SAS token credentials
            Console.WriteLine("Creating SAS credential.");

            TimeSpan timeToLive = TimeSpan.FromHours(1);
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            string sasToken = GenerateSasToken(parameters.HostName, parameters.SharedAccessKey, parameters.SharedAccessKeyName, expiresOn);
            // Note: Pass the generated sasToken and not just the shared access signature when creating the AzureSasCredential.
            AzureSasCredential sasCredential = new AzureSasCredential(sasToken);

            // This is how the credential can be updated in the AzureSasCredential object whenever necessary.
            // This sample just shows how to perform the update but it is not necessary to update the token
            // until the token is close to its expiry.
            DateTime newExpiresOn = DateTime.UtcNow.Add(timeToLive);
            string updatedSasToken = GenerateSasToken(parameters.HostName, parameters.SharedAccessKey, parameters.SharedAccessKeyName, newExpiresOn);
            sasCredential.Update(updatedSasToken);

            ProvisioningServiceClient provisioningServiceClient = ProvisioningServiceClient.Create(parameters.HostName, sasCredential);

            var sample = new ProvisioningAzureSasCredentialSample(provisioningServiceClient);
            await sample.RunSampleAsync();
        }

        private static string GenerateSasToken(string resourceUri, string sharedAccessKey, string policyName, DateTime expiresOn)
        {
            DateTime epochTime = new DateTime(1970, 1, 1);
            TimeSpan secondsFromEpochTime = expiresOn.Subtract(epochTime);
            long seconds = Convert.ToInt64(secondsFromEpochTime.TotalSeconds, CultureInfo.InvariantCulture);
            string expiry = Convert.ToString(seconds, CultureInfo.InvariantCulture);

            string stringToSign = WebUtility.UrlEncode(resourceUri) + "\n" + expiry;

            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(sharedAccessKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]
            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(resourceUri),
                WebUtility.UrlEncode(signature),
                expiry);

            if (!string.IsNullOrWhiteSpace(policyName))
            {
                token += "&skn=" + policyName;
            }

            return token;
        }
    }
}
