// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample for illustrating how a device should handle connection status updates.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        public static async Task<int> Main(string[] args)
        {
            // Parse application parameters
            Parameters? parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            // Set up logging
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                });
            var logger = loggerFactory.CreateLogger<Program>();

            const string SdkEventProviderPrefix = "Microsoft-Azure-";
            // Instantiating this seems to do all we need for outputting SDK events to our console log
            // SDK events are logged at trace log level.
            _ = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            // Run the sample
            var runningTime = parameters?.ApplicationRunningTime != null
                ? TimeSpan.FromSeconds((double)parameters.ApplicationRunningTime)
                : Timeout.InfiniteTimeSpan;

            if (parameters == null)
            {
                logger.LogError("No input parameters received. Exiting sample.");
                return 0;
            }

            var sample = new PnpUsabilitySample(parameters.PrimaryConnectionString, parameters.TransportType, logger);
            await sample.RunSampleAsync(runningTime);

            logger.LogInformation("Done.");
            return 0;
        }
    }
}
