using Azure;
using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Service;
using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ProvisioningAzureSasCredentialSample
{
    internal class Program
    {
        private static TimeSpan timeToLive = TimeSpan.FromHours(1);
        private static string sasToken;
        private static AzureSasCredential sasCredential;
        private static Parameters parameters = null;

        public static async Task Main(string[] args)
        {
            // Parse application parameters
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

            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            sasToken = GenerateSasToken(parameters.HostName, parameters.SharedAccessKey, parameters.SharedAccessKeyName, expiresOn);
            // Note: Pass the generated sasToken and not just the shared access signature when creating the AzureSasCredential.
            sasCredential = new AzureSasCredential(sasToken);

            // This is an example of how to create a Timer that will automatically update the SAS credential every hour, to avoid expiry
            // Since this sample is short, this timer event will not trigger, and thus this section can be ignored/removed
            Timer updateTimer = new Timer();
            updateTimer.Interval = 3600000;
            updateTimer.Elapsed += UpdateToken;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;

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

        private static void UpdateToken(Object source, ElapsedEventArgs e)
        {
            DateTime newExpiresOn = DateTime.UtcNow.Add(timeToLive);
            string updatedSasToken = GenerateSasToken(parameters.HostName, parameters.SharedAccessKey, parameters.SharedAccessKeyName, newExpiresOn);
            sasCredential.Update(updatedSasToken);
        }
    }
}
