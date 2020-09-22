// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        private static ILogger s_logger;

        /// <summary>
        /// A sample to illustrate how to perform root level service-side operations on a plug and play compatible device.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        /// <remarks>
        /// This sample performs operations on a plug and play compatible device on the root level. For a sample on how to perform
        /// operations on a component level on a plug an dplay compatible device, please check out <see href="https://www.bing.com/">TODO FILL THIS IN</see>.
        /// </remarks> 
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

            s_logger = InitializeConsoleDebugLogger();

            s_logger.LogDebug("Set up the digital twin client.");
            using DigitalTwinClient digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(parameters.HubConnectionString);

            s_logger.LogDebug("Set up and start the Thermostat sample.");
            var thermostatSample = new ThermostatSample(digitalTwinClient, parameters.DigitalTwinId, s_logger);
            await thermostatSample.RunSampleAsync().ConfigureAwait(false);
        }

        private static ILogger InitializeConsoleDebugLogger()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });

            return loggerFactory.CreateLogger<ThermostatSample>();
        }
    }
}
