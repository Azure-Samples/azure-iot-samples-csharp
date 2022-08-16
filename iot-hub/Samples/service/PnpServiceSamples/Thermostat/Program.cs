// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        /// <summary>
        /// This sample performs root-level operations on a plug and play compatible device using the IoT Hub service client
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

            var thermostatSample = new ThermostatSample(serviceClient, registryManager, parameters.DeviceId);
            await thermostatSample.RunSampleAsync().ConfigureAwait(false);
        }
    }
}
