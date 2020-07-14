﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//This code that messages to an IoT Hub for testing the routing as defined
//  in this article: https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing
//The scripts for creating the resources are included in the resources folder in this
//  Visual Studio solution. 

using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace arm_read_write
{
    class Program
    {
        //
        //    1. Use the ARM template to load the resources for this quickstart.
        //    2. Using CLI, create a device on the IoT Hub. Retrieve the keys and save them.
        //    3. Set the routing query on the device to be "true" so all messages will be passed through 
        //        and written to Azure Storage.:
        //    4. run the simulated device application (arm-read-write). It will submit messages to the hub, 
        //        which will write them all to storage.
        //    5. open the storage account and look at the resulting messages
        //    6. Stop running the console app
        //    7. When finished, delete the resource group
        //
        // ARM template is called arm-read-write-sample.
        // put this in the repository so you can just call it to load it 
        // keep the suffixes so you don't have to worry about duplicates

        //This is the code that sends messages to the IoT Hub for routing messages to storage. 
        //  In this case, all the messages will be routed directly to storage and written.
        //  This was derived by the (more complicated) tutorial for routing 
        //  https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing

        private static DeviceClient s_deviceClient;
        private readonly static string s_myDeviceId = "Contoso-Test-Device";
        private readonly static string s_iotHubUri = "ContosoTestHubdlxlud5h.azure-devices.net";
        // This is the primary key for the device. This is in the portal. 
        // Find your IoT hub in the portal > IoT devic1es > select your device > copy the key. 
        private readonly static string s_deviceKey = "RClD0LGxZCYavagk8tS2M7L1MI5bcKcyR+tJHzj+gDk=";

        private static async Task Main()
        {
            Console.WriteLine("write messages to a hub and use routing to write them to storage");
            s_deviceClient = DeviceClient.Create(s_iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(s_myDeviceId, s_deviceKey), TransportType.Mqtt);

            var cts = new CancellationTokenSource();

            var messages = SendDeviceToCloudMessagesAsync(cts.Token);
            Console.WriteLine("Press the Enter key to stop.");
            Console.ReadLine();
            cts.Cancel();
            await messages;
        }

        /// <summary>
        /// Send message to the Iot hub. This generates the object to be sent to the hub in the message.
        /// </summary>
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken token)
        {
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            while (!token.IsCancellationRequested)
            {

                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                string infoString;
                string levelValue;

                if (rand.NextDouble() > 0.7)
                {
                    if (rand.NextDouble() > 0.5)
                    {
                        levelValue = "critical";
                        infoString = "This is a critical message.";
                    }
                    else
                    {
                        levelValue = "storage";
                        infoString = "This is a storage message.";
                    }
                }
                else
                {
                    levelValue = "normal";
                    infoString = "This is a normal message.";
                }

                var telemetryDataPoint = new
                {
                    deviceId = s_myDeviceId,
                    temperature = currentTemperature,
                    humidity = currentHumidity,
                    pointInfo = infoString
                };
                // serialize the telemetry data and convert it to JSON.
                var telemetryDataString = JsonConvert.SerializeObject(telemetryDataPoint);

                // Encode the serialized object using UTF-32. When it writes this to a file, 
                //   it encodes it as base64. If you read it back in, you have to decode it from base64 
                //   and utf-32 to be able to read it.

                // You can encode this as ASCII, but if you want it to be the body of the message, 
                //  and to be able to search the body, it must be encoded in UTF with base64 encoding.

                // Take the string (telemetryDataString) and turn it into a byte array 
                //   that is encoded as UTF-32.
                var message = new Message(Encoding.UTF32.GetBytes(telemetryDataString));

                //Add one property to the message.
                message.Properties.Add("level", levelValue);

                // Submit the message to the hub.
                await s_deviceClient.SendEventAsync(message);

                // Print out the message.
                Console.WriteLine("{0} > Sent message: {1}", DateTime.Now, telemetryDataString);

                await Task.Delay(1000);
            }
        }
    }
}
