// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to manage enrollment groups in device provisioning service.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        private static string s_connectionString = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");

        public static int Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            X509Certificate2 certificate = new X509Certificate2(parameters.CertificatePath);

            // The ProvisioningConnectionString argument is not required when either:
            // - set the PROVISIONING_CONNECTION_STRING environment variable 
            // - create a launchSettings.json (see launchSettings.json.template) containing the variable
            if (string.IsNullOrEmpty(s_connectionString) && !string.IsNullOrEmpty(parameters.ProvisioningConnectionString))
            {
                s_connectionString = parameters.ProvisioningConnectionString;
            }
           
            using (var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(s_connectionString))
            {
                var sample = new EnrollmentGroupSample(provisioningServiceClient, certificate);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
