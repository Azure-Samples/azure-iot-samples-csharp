using Azure.Core;
using Azure.Identity;
using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Service;
using System;
using System.Threading.Tasks;

namespace ProvisioningRoleBasedAuthenticationSample
{
    public class Program
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

            // Initialize Azure active directory credentials.
            Console.WriteLine("Creating token credential.");

            // These environment variables are necessary for DefaultAzureCredential to use application Id and client secret to login.
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", parameters.ClientSecret);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", parameters.ClientId);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", parameters.TenantId);

            // DefaultAzureCredential supports different authentication mechanisms and determines the appropriate credential type based of the environment it is executing in.
            // It attempts to use multiple credential types in an order until it finds a working credential.
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/azure.identity?view=azure-dotnet.
            TokenCredential tokenCredential = new DefaultAzureCredential();

            //ProvisioningServiceClient provisioningServiceClient = ProvisioningServiceClient.Create(parameters.HostName, tokenCredential);
            ProvisioningServiceClient provisioningServiceClient = null;

            var sample = new ProvisioningRoleBasedAuthenticationSample(provisioningServiceClient);
            await sample.RunSampleAsync();
        }
    }
}
