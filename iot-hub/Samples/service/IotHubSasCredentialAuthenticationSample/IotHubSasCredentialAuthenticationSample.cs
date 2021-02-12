// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IotHubSasCredentialAuthenticationSample
{
    /// <summary>
    /// This sample connects to the IoT hub using SAS token and sends a cloud-to-device message.
    /// </summary>
    public class IotHubSasCredentialAuthenticationSample
    {
        public async Task RunSampleAsync(ServiceClient client, string deviceId)
        {
            Console.WriteLine("Connecting using SAS credential.");
            await client.OpenAsync();
            Console.WriteLine("Successfully opened connection.");

            Console.WriteLine("Sending a cloud-to-device message.");
            using var message = new Message(Encoding.ASCII.GetBytes("Hello, Cloud!"));
            await client.SendAsync(deviceId, message);
            Console.WriteLine("Successfully sent message.");
        }
    }
}
