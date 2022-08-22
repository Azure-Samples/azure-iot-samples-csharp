// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using CommandLine;

namespace SimulatedDevice
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry. For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/device/DeviceReconnectionSample"/>.
    /// </summary>
    internal class Program
    {
        private static Parameters s_parameters;
        private static DeviceClient s_deviceClient;
        private static readonly TransportType s_transportType = TransportType.Mqtt;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string s_connectionString;

        private static async Task Main(string[] args)
        {
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams => s_parameters = parsedParams)
                .WithNotParsed(errors => Environment.Exit(1));

            // The device connection string must be specified in Parameters, or program will exit
            if (string.IsNullOrWhiteSpace(s_parameters.DeviceConnectionString))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            s_connectionString = s_parameters.DeviceConnectionString;

            Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device.");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            // Set up a condition to quit the sample
            Console.WriteLine("Press control-C to exit.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Run the telemetry loop
            await SendDeviceToCloudMessagesAsync(cts.Token);

            // SendDeviceToCloudMessagesAsync is designed to run until cancellation has been explicitly requested by Console.CancelKeyPress.
            // As a result, by the time the control reaches the call to close the device client, the cancellation token source would
            // have already had cancellation requested.
            // Hence, if you want to pass a cancellation token to any subsequent calls, a new token needs to be generated.
            // For device client APIs, you can also call them without a cancellation token, which will set a default
            // cancellation timeout of 4 minutes: https://github.com/Azure/azure-iot-sdk-csharp/blob/64f6e9f24371bc40ab3ec7a8b8accbfb537f0fe1/iothub/device/src/InternalClient.cs#L1922
            await s_deviceClient.CloseAsync();

            s_deviceClient.Dispose();
            Console.WriteLine("Device simulator finished.");
        }

        // Async method to send simulated telemetry
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            var rand = new Random();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double currentTemperature = minTemperature + rand.NextDouble() * 15;
                    double currentHumidity = minHumidity + rand.NextDouble() * 20;

                    // Create JSON message
                    string messageBody = JsonSerializer.Serialize(
                        new
                        {
                            temperature = currentTemperature,
                            humidity = currentHumidity,
                        });
                    using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                    {
                        ContentType = "application/json",
                        ContentEncoding = "utf-8",
                    };

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                    // Send the telemetry message
                    await s_deviceClient.SendEventAsync(message, ct);
                    Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");

                    await Task.Delay(1000, ct);
                }
            }
            catch (TaskCanceledException) { } // ct was signaled
        }
    }
}
