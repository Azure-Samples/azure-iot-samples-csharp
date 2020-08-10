// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceReconnectionSample
    {
        private static readonly Random s_randomGenerator = new Random();
        private const int TemperatureThreshold = 30;
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_operationTimeout = TimeSpan.FromHours(1);

        private readonly string _deviceConnectionString;
        private readonly TransportType _transportType;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private static DeviceClient s_deviceClient;
        private static ConnectionStatus connectionStatus;
        private static bool wasConnected;

        public DeviceReconnectionSample(string deviceConnectionString, TransportType transportType, ILogger logger)
        {
            _logger = logger;
            _deviceConnectionString = deviceConnectionString;
            _transportType = transportType;

            InitializeClient();
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");

            connectionStatus = status;
            switch (connectionStatus)
            {
                case ConnectionStatus.Connected:
                    _logger.LogDebug("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");

                    wasConnected = true;
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    _logger.LogDebug("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance");
                    break;

                case ConnectionStatus.Disabled:
                    _logger.LogDebug("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                    InitializeClient();
                    break;

                case ConnectionStatus.Disconnected:
                    {
                        if (reason == ConnectionStatusChangeReason.Bad_Credential)
                        {
                            _logger.LogWarning("### The supplied credentials were invalid." +
                                "\nFix the input and then create a new device client instance.");
                        }
                        else if (reason == ConnectionStatusChangeReason.Device_Disabled)
                        {
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                        }
                        else if (reason == ConnectionStatusChangeReason.Retry_Expired)
                        {
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                        }
                        else if (reason == ConnectionStatusChangeReason.Communication_Error)
                        {
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                        }
                    }
                    break;
            }
        }

        public async Task RunSampleAsync()
        {
            await Task.WhenAll(SendMessagesAsync(), ReceiveMessagesAsync());
        }

        private void InitializeClient()
        {
            // If the client reports Connected status, it is already in operational state.
            if (connectionStatus != ConnectionStatus.Connected)
            {
                lock (_lock)
                {
                    _logger.LogDebug($"Attempting to reinitialize the client instance, current status={connectionStatus}");

                    // If the device client instance has been previously initialized, then dispose it.
                    // The wasConnected variable is required to store if the client ever reported Connected status.
                    if (wasConnected && connectionStatus == ConnectionStatus.Disconnected)
                    {
                        s_deviceClient?.Dispose();
                        wasConnected = false;
                    }

                    s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType);
                    s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                    s_deviceClient.OperationTimeoutInMilliseconds = (uint)s_operationTimeout.TotalMilliseconds;
                    _logger.LogDebug($"Reinitialized the client instance.");
                }
            }
        }

        private async Task SendMessagesAsync()
        {
            int count = 0;
            while (true)
            {
                count++;
                _logger.LogInformation($"Device sending message {count} to IoTHub...");

                try
                {
                    await SendMessageAsync(count);
                }
                catch (Exception ex) when (ex is IotHubException exception && exception.IsTransient)
                {
                    // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                    _logger.LogError($"An IotHubException was caught, but will try to recover and retry explicitly: {ex}");
                }
                catch (Exception ex)
                {
                    if (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but will try to recover and retry explicitly: {ex}");
                    }
                    else
                    {
                        throw;
                    }
                }

                await Task.Delay((int)s_sleepDuration.TotalMilliseconds);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            while (true)
            {
                _logger.LogInformation($"Device waiting for C2D messages from the hub - for {s_sleepDuration}...");
                _logger.LogInformation("Use the IoT Hub Azure Portal or Azure IoT Explorer to send a message to this device.");

                try
                {
                    await ReceiveMessageAndCompleteAsync();
                }
                catch (Exception ex) when (ex is DeviceMessageLockLostException)
                {
                    _logger.LogWarning($"Attempted to complete a received message whose lock token has expired; ignoring: {ex}");
                }
                catch (Exception ex) when (ex is IotHubException exception && exception.IsTransient)
                {
                    // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                    _logger.LogError($"An IotHubException was caught, but will try to recover and retry explicitly: {ex}");
                }
                catch (Exception ex)
                {
                    if (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but will try to recover and retry explicitly: {ex}");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private async Task SendMessageAsync(int count)
        {
            var _temperature = s_randomGenerator.Next(20, 35);
            var _humidity = s_randomGenerator.Next(60, 80);
            string dataBuffer = $"{{\"temperature\":{_temperature},\"humidity\":{_humidity}}}";

            using var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer))
            {
                MessageId = count.ToString()
            };
            eventMessage.Properties.Add("temperatureAlert", (_temperature > TemperatureThreshold) ? "true" : "false");

            _logger.LogInformation($"Sending message: {count}, Data: [{dataBuffer}]");
            await s_deviceClient.SendEventAsync(eventMessage);
        }

        private async Task ReceiveMessageAndCompleteAsync()
        {
            using Message receivedMessage = await s_deviceClient.ReceiveAsync(s_sleepDuration).ConfigureAwait(false);
            if (receivedMessage != null)
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                _logger.LogInformation($"Received message: {messageData}");

                int propCount = 0;
                foreach (var prop in receivedMessage.Properties)
                {
                    _logger.LogInformation($"Property[{propCount++}> Key={prop.Key} : Value={prop.Value}");
                }

                await s_deviceClient.CompleteAsync(receivedMessage).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("No message received, timed out");
            }
        }
    }
}
