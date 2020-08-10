// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        private const string SdkEventProviderPrefix = "Microsoft-Azure-";

        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";

        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_DEVICE_CONN_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");

        // Select one of the following transports used by DeviceClient to connect to IoT Hub.
        private static TransportType s_transportType = TransportType.Amqp;
        //private static TransportType s_transportType = TransportType.Mqtt;
        //private static TransportType s_transportType = TransportType.Amqp_WebSocket_Only;
        //private static TransportType s_transportType = TransportType.Mqtt_WebSocket_Only;

        public static async Task<int> Main(string[] args)
        {
            // Create a console logger, that logs all events that are categorized at Debug level or higher.
            // For additional details, see https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-3.1.
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug);
            });

            loggerFactory
                .AddColorConsoleLogger(c =>
                {
                    c.LogLevel = LogLevel.Trace;
                    c.Color = ConsoleColor.Blue;
                })
                .AddColorConsoleLogger(c =>
                {
                    c.LogLevel = LogLevel.Debug;
                    c.Color = ConsoleColor.DarkYellow;

                })
                .AddColorConsoleLogger(c =>
                {
                    c.LogLevel = LogLevel.Information;
                    c.Color = ConsoleColor.Cyan;
                })
                .AddColorConsoleLogger(c =>
                {
                    c.LogLevel = LogLevel.Warning;
                    c.Color = ConsoleColor.DarkMagenta;
                })
                .AddColorConsoleLogger(c =>
                {
                    c.LogLevel = LogLevel.Error;
                    c.Color = ConsoleColor.Red;
                })
                .AddColorConsoleLogger(c =>
                {
                    c.LogLevel = LogLevel.Critical;
                    c.Color = ConsoleColor.DarkRed;
                });
            var logger = loggerFactory.CreateLogger<Program>();

            var sdkEventListener = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            if (string.IsNullOrEmpty(s_deviceConnectionString) && args.Length > 0)
            {
                s_deviceConnectionString = args[0];
            }

            var sample = new DeviceReconnectionSample(s_deviceConnectionString, s_transportType, logger);
            await sample.RunSampleAsync();

            logger.LogInformation("Done, exiting...");
            return 0;
        }
    }
}
