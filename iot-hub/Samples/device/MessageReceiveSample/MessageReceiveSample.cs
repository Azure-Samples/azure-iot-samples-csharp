// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class MessageReceiveSample
    {
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_receiveTimeout = TimeSpan.FromSeconds(10);
        private readonly DeviceClient _deviceClient;
        private readonly TransportType _transportType;

        public MessageReceiveSample(DeviceClient deviceClient, TransportType transportType)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            _transportType = transportType;
        }

        public async Task RunSampleAsync()
        {
            // First receive messages using the polling ReceiveAsync().
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub for {s_receiveTimeout}...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");
            await ReceiveC2dMessagesPollingAndComplete(s_receiveTimeout);

            // Now subscribe to receive C2D messages through a callback.
            await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceived, _deviceClient);
            Console.WriteLine($"\n{DateTime.Now}> Subscribed to receive C2D messages over callback.");

            // Now wait to receive message from service.
            // The message should be received on the callback, while the polling ReceiveAsync() call should return null.
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub for {s_receiveTimeout}...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");
            await ReceiveC2dMessagesPollingAndComplete(s_receiveTimeout);

            // Now unsubscribe from the callback.
            await _deviceClient.SetReceiveMessageHandlerAsync(null, _deviceClient);
            Console.WriteLine($"\n{DateTime.Now}> Unsubscribed from receiving C2D messages over callback.");

            // For Mqtt - since we have explicitly unsubscribed, we will need to resubscribe again
            // before the device can begin receiving C2D messages.
            if (_transportType == TransportType.Mqtt_Tcp_Only
                || _transportType == TransportType.Mqtt_WebSocket_Only)
            {
                Message leftoverMessage = await _deviceClient.ReceiveAsync(s_sleepDuration).ConfigureAwait(false);
            }

            // Now wait to receive message from service.
            // The message should be received on the polling ReceiveAsync() call and not on the callback.
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub for {s_receiveTimeout}...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");
            await ReceiveC2dMessagesPollingAndComplete(s_receiveTimeout);
        }

        private async Task ReceiveC2dMessagesPollingAndComplete(TimeSpan timeout)
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"{DateTime.Now}> Receiving C2D messages on the polling ReceiveAsync().");
            while (sw.Elapsed < timeout)
            {
                using Message receivedMessage = await _deviceClient.ReceiveAsync(timeout);

                if (receivedMessage == null)
                {
                    Console.WriteLine($"{DateTime.Now}> Polling ReceiveAsync() - no message received.");
                    await Task.Delay(s_sleepDuration);
                    continue;
                }

                Console.WriteLine($"{DateTime.Now}> Polling ReceiveAsync() - received message with Id={receivedMessage?.MessageId}");
                ProcessReceivedMessage(receivedMessage);

                await _deviceClient.CompleteAsync(receivedMessage);
                Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage?.MessageId}.");
            }

            sw.Stop();
        }

        private async Task OnC2dMessageReceived(Message receivedMessage, object _)
        {
            Console.WriteLine($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage?.MessageId}.");
            ProcessReceivedMessage(receivedMessage);

            await _deviceClient.CompleteAsync(receivedMessage);
            Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage?.MessageId}.");
        }

        private void ProcessReceivedMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            var formattedMessage = new StringBuilder($"Received message: [{messageData}], Id={receivedMessage?.MessageId}");
            foreach (var prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\nProperty: key={prop.Key}, value={prop.Value}");
            }
            Console.WriteLine($"{DateTime.Now}> {formattedMessage}");
        }
    }
}
