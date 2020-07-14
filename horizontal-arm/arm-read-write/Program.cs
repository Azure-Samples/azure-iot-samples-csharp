// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//This code that messages to an IoT Hub for testing the the arm-read-write app is
//  in Azure-IoT-Samples-CSharp/horizontal-arm/arm-read-write//
//The ARM template is in the .\arm-template folder.

using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace arm_read_write
{
    class Program
    {
        //
        //    This is how this tutorial will work. 
        //
        //    1. Use the ARM template to load the resources for this quickstart.
        //    2. Create a device on the IoT Hub. Retrieve the device id keys and save them.
        //    3. Set the routing query on the device to be "true" so all messages will be passed through 
        //        and written to Azure Storage.
        //    4. run this application (arm-read-write). It will send messages to the hub, 
        //        which will write them all to storage.
        //    5. open the storage account and look at the resulting messages
        //    6. Stop running the console app
        //    7. When finished, delete the resource group
        //

        //This is the code that sends messages to the IoT Hub for routing messages to storage. 
        //  In this case, all the messages will be routed directly to storage and written.
        //  This was derived by the (more complicated) tutorial for routing 
        //  https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing

        private static DeviceClient s_deviceClient;

        //These are environment variables, retrieve them and store them here.
        // This is the primary key for the device. This is in the portal. 
        // Find your IoT hub in the portal > IoT devices > select your device > copy the key. 
        private static string _envIOT_DEVICE_ID = Environment.GetEnvironmentVariable("IOT_DEVICE_ID");
        private static string _envIOT_HUB_URI = Environment.GetEnvironmentVariable("IOT_HUB_URI");
        private static string _envIOT_DEVICE_KEY = Environment.GetEnvironmentVariable("IOT_DEVICE_KEY");

        private static void Main()
        {
            //for testing, just hardcode these
            _envIOT_HUB_URI = "IOT-HUB-NAME-GOES-HERE.azure-devices-net";
            _envIOT_DEVICE_KEY = "IOT-DEVICE-KEY-GOES-HERE";
            _envIOT_DEVICE_ID = "Contoso-Test-Device";
            Console.WriteLine("iot hub uri = <{0}>", _envIOT_HUB_URI);
            Console.WriteLine("iot device id = <{0}>", _envIOT_DEVICE_ID);
            Console.WriteLine("iot device key = <{0}>", _envIOT_DEVICE_KEY);

            Console.WriteLine("starting");
            s_deviceClient = DeviceClient.Create(_envIOT_HUB_URI, 
                new DeviceAuthenticationWithRegistrySymmetricKey(_envIOT_DEVICE_ID, _envIOT_DEVICE_KEY), 
                TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();

            Console.WriteLine("Press the Enter key to stop.");

            Console.ReadLine();


        }

        /// <summary>
        /// Send message to the Iot hub. This generates the object to be sent to the hub in the message.
        /// </summary>
        private static async void SendDeviceToCloudMessagesAsync()
        {
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            while (true)
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
                    deviceId = _envIOT_DEVICE_ID,
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
