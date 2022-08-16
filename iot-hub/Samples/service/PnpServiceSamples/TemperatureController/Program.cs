// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        /// <summary>
        /// This sample performs component-level operations on an IoT device using the ServiceClient APIs.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        public static async Task Main(string[] args)
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

            if (!parameters.Validate())
            {
                throw new ArgumentException("Required parameters are not set. Please recheck required variables by using \"--help\"");
            }

            using ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(parameters.HubConnectionString);
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(parameters.HubConnectionString);

            var temperatureControllerSample = new TemperatureControllerSample(serviceClient, registryManager, parameters.DeviceId);
            await temperatureControllerSample.RunSampleAsync().ConfigureAwait(false);
        }
    }
}
