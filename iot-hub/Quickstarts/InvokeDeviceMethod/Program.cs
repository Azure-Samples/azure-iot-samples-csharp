// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub service SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/service

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace InvokeDeviceMethod
{
    /// <summary>
    /// This sample illustrates the very basics of a service app invoking a method on a device.
    /// </summary>
    internal class Program
    {
        private static ServiceClient s_serviceClient;
        
        // Connection string for your IoT Hub
        // az iot hub show-connection-string --hub-name {your iot hub name} --policy-name service
        // Override by passing service connection string as first parameter in command line
        private static string s_connectionString = "{Your service connection string here}";

        // Default device name is `MyDotnetDevice`
        // Override by passing custom name as second parameter in command line
        private static string deviceName = "MyDotnetDevice";

        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts #2 - InvokeDeviceMethod application.");

            // This sample accepts the service connection string as a parameter, if present
            ValidateConnectionString(args);

            // This sample accepts a device name as a parameter, if present
            ValidateDeviceName(args);

            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            s_serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString);

            await InvokeMethodAsync();

            s_serviceClient.Dispose();

            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        // Invoke the direct method on the device, passing the payload
        private static async Task InvokeMethodAsync()
        {
            var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            methodInvocation.SetPayloadJson("10");

            Console.WriteLine($"\nInvoking direct method for device: {deviceName}");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await s_serviceClient.InvokeDeviceMethodAsync(deviceName, methodInvocation);

            Console.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");

        }

        private static void ValidateConnectionString(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    var cs = IotHubConnectionStringBuilder.Create(args[0]);
                    s_connectionString = cs.ToString();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error: Unrecognizable parameter '{args[0]}' as connection string.");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    _ = IotHubConnectionStringBuilder.Create(s_connectionString);
                }
                catch (Exception)
                {
                    Console.WriteLine("This sample needs a device connection string to run. Program.cs can be edited to specify it, or it can be included on the command-line as the only parameter.");
                    Environment.Exit(1);
                }
            }
        }

        // Update deviceName variable if a second argument was passed in
        private static void ValidateDeviceName(string[] args)
        {
            if (args.Length > 1)
            {
                deviceName = args[1];
            }
        }
    }
}
