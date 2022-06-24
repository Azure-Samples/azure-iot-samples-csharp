// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a symmetric key as
    /// onboarding authentication mechanism and passing in a certificate signing request, and then use the 
    /// issued device certificate to authenticate to IoT hub.
    /// </summary>
    public class Program
    {
        public static async Task<int> Main(string[] args)
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

            // Set up logging
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    MinLogLevel = LogLevel.Trace,
                });
            var logger = loggerFactory.CreateLogger<Program>();

            const string SdkEventProviderPrefix = "Microsoft-Azure-";
            // Instantiating this seems to do all we need for outputting SDK events to our console log
            // The SDK events are written at trace log level.
            _ = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            var sample = new ConnectUsingOperationalCertificateSample(parameters, logger);
            await sample.RunSampleAsync();

            return 0;
        }
    }
}
