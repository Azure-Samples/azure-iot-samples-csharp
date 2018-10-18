// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class Program
    {
        // The Provisioning Service connection string. This is available under the "Shared access policies" in the Azure portal.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the PROVISIONING_CONNECTION_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_connectionString = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_connectionString) && args.Length > 0)
            {
                s_connectionString = args[0];
            }

            ServiceConnectionString serviceConnectionString = ServiceConnectionString.Parse(s_connectionString);
            var dpsUri = serviceConnectionString.HttpsEndpoint;
            ServiceClientCredentials credentials;

            if (serviceConnectionString.SharedAccessSignature != null)
            {
                credentials = new SharedAccessSignatureCredentials(serviceConnectionString.SharedAccessSignature);
            }
            else
            {
                credentials = new SharedAccessKeyCredentials(serviceConnectionString);
            }

            using (var provisioningServiceClient = new ProvisioningServiceClient(dpsUri, credentials))
            {
                var sample = new EnrollmentSample(provisioningServiceClient);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
