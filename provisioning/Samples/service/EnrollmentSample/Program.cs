// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;
using EnrollmentSample;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class Program
    {
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

            if (string.IsNullOrEmpty(parameters.ProvisioningConnectionString) && args.Length > 0)
            {
                parameters.ProvisioningConnectionString = args[0];
            }

            using (var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(parameters.ProvisioningConnectionString))
            {
                var sample = new EnrollmentSample(provisioningServiceClient);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
