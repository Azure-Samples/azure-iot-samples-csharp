// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceReconnectionSample
    {
        private static readonly Random s_randomGenerator = new Random();
        private const int TemperatureThreshold = 30;
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly SemaphoreSlim s_initializeClientSemaphore = new SemaphoreSlim(1, 1);

        private readonly string _deviceConnectionString;
        private readonly TransportType _transportType;
        private readonly ILogger _logger;

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile DeviceClient s_deviceClient;
        private static volatile ConnectionStatus s_connectionStatus;

        public DeviceReconnectionSample(string deviceConnectionString, TransportType transportType, ILogger logger)
        {
            _logger = logger;
            _deviceConnectionString = deviceConnectionString;
            _transportType = transportType;

            InitializeClient().GetAwaiter().GetResult();
        }

        public async Task RunSampleAsync()
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation("Sample execution cancellation requested, will exit.");
            };

            try
            {
                await Task.WhenAll(SendMessagesAsync(cts.Token), ReceiveMessagesAsync(cts.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }
        }

        private async void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");

            s_connectionStatus = status;
            switch (s_connectionStatus)
            {
                case ConnectionStatus.Connected:
                    _logger.LogDebug("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    _logger.LogDebug("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance");
                    break;

                case ConnectionStatus.Disabled:
                    _logger.LogDebug("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.Bad_Credential:
                            _logger.LogWarning("### The supplied credentials were invalid." +
                                "\nFix the input and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeClient();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeClient();
                            break;

                        default:
                            _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                            break;

                    }

                    break;

                default:
                    _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                    break;
            }
        }

        private async Task InitializeClient()
        {
            if (ClientShouldBeInitialized(s_connectionStatus))
            {
                // Allow a single thread to dispose and initialize the client instance.
                await s_initializeClientSemaphore.WaitAsync();
                if (ClientShouldBeInitialized(s_connectionStatus))
                {
                    _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                    if (s_connectionStatus == ConnectionStatus.Disconnected)
                    {
                        // If the device client instance has been previously initialized, then dispose it.
                        if (s_deviceClient != null)
                        {
                            s_deviceClient.Dispose();
                            s_deviceClient = null;
                        }
                    }

                    s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType);
                    s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

                    _logger.LogDebug($"Initialized the client instance.");
                }
                s_initializeClientSemaphore.Release();
            }

            // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
            await s_deviceClient.OpenAsync();
            _logger.LogDebug($"Successfully opened the client instance.");
        }

        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            int count = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                // If the client is in Connected state, perform the desired operation.
                if (s_connectionStatus == ConnectionStatus.Connected)
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
                    catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but will try to recover and retry explicitly: {ex}");
                    }

                    await Task.Delay(s_sleepDuration);
                }
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // If the client is in Connected state, perform the desired operation.
                if (s_connectionStatus == ConnectionStatus.Connected)
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
                    catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but will try to recover and retry explicitly: {ex}");
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
                MessageId = count.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };
            eventMessage.Properties.Add("temperatureAlert", (_temperature > TemperatureThreshold) ? "true" : "false");

            await s_deviceClient.SendEventAsync(eventMessage);
            _logger.LogInformation($"Sent message: {count}, Data: [{dataBuffer}]");
        }

        private async Task ReceiveMessageAndCompleteAsync()
        {
            using Message receivedMessage = await s_deviceClient.ReceiveAsync(s_sleepDuration);
            if (receivedMessage != null)
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                _logger.LogInformation($"Received message: {messageData}");

                int propCount = 0;
                foreach (var prop in receivedMessage.Properties)
                {
                    _logger.LogInformation($"Property[{propCount++}> Key={prop.Key} : Value={prop.Value}");
                }

                await s_deviceClient.CompleteAsync(receivedMessage);
                _logger.LogInformation($"Marked message [{messageData}] as \"Complete\".");
            }
            else
            {
                _logger.LogWarning("No message received, timed out");
            }
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying state, it is trying to recover its connection.
        private static bool ClientShouldBeInitialized(ConnectionStatus connectionStatus)
        {
            return connectionStatus != ConnectionStatus.Connected && connectionStatus != ConnectionStatus.Disconnected_Retrying;
        }
    }
}
