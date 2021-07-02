// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceReconnectionSample
    {
        private const int TemperatureThreshold = 30;
        private static readonly Random s_randomGenerator = new Random();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_deviceOperationTimeout = TimeSpan.FromSeconds(30);

        private readonly object _initLock = new object();
        private readonly List<string> _deviceConnectionStrings;
        private readonly TransportType _transportType;
        private readonly ClientOptions _clientOptions = new ClientOptions { SdkAssignsMessageId = Shared.SdkAssignsMessageId.WhenUnset };

        private readonly ILogger _logger;
        private readonly ConcurrentQueue<Message> _receivedMessagesQueue = new ConcurrentQueue<Message>();

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile DeviceClient s_deviceClient;
        private static volatile ConnectionStatus s_connectionStatus = ConnectionStatus.Disconnected;

        public DeviceReconnectionSample(List<string> deviceConnectionStrings, TransportType transportType, ILogger logger)
        {
            _logger = logger;

            // This class takes a list of potentially valid connection strings (most likely the currently known good primary and secondary keys)
            // and will attempt to connect with the first. If it receives feedback that a connection string is invalid, it will discard it, and
            // if any more are remaining, will try the next one.
            // To test this, either pass an invalid connection string as the first one, or rotate it while the sample is running, and wait about
            // 5 minutes.
            if (deviceConnectionStrings == null
                || !deviceConnectionStrings.Any())
            {
                throw new ArgumentException("At least one connection string must be provided.", nameof(deviceConnectionStrings));
            }
            _deviceConnectionStrings = deviceConnectionStrings;
            _logger.LogInformation($"Supplied with {_deviceConnectionStrings.Count} connection string(s).");

            _transportType = transportType;
            _logger.LogInformation($"Using {_transportType} transport.");
        }

        private bool IsDeviceConnected => s_connectionStatus == ConnectionStatus.Connected;

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            using var cts = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            _logger.LogInformation($"Sample execution started, press Control+C to quit the sample.");

            try
            {
                await InitializeAndOpenClientAsync();

                var tasks = new List<Task> { SendMessagesAsync(cts.Token), CompleteReceivedMessagesAsync(cts.Token) };
                if (_transportType == TransportType.Http1)
                {
                    tasks.Add(ReceiveMessagesAsync(cts.Token));
                }

                await TaskHelper.WhenAllFailFast(tasks.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }
        }

        private async Task InitializeAndOpenClientAsync()
        {
            if (ShouldClientBeInitialized(s_connectionStatus))
            {
                // Allow a single thread to dispose and initialize the client instance.
                lock (_initLock)
                {
                    if (ShouldClientBeInitialized(s_connectionStatus))
                    {
                        _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                        // If the device client instance has been previously initialized, then dispose it.
                        if (s_deviceClient != null)
                        {
                            s_deviceClient.Dispose();
                            s_deviceClient = null;
                        }
                    }

                    s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionStrings.First(), _transportType, _clientOptions);
                    s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

                    // Updating the client's operation timeout to 30secs for the purpose of a sample. The default value is 4mins.
                    s_deviceClient.OperationTimeoutInMilliseconds = (uint)s_deviceOperationTimeout.TotalMilliseconds;
                    _logger.LogDebug($"Initialized the client instance.");
                }

                try
                {
                    // Force connection now.
                    // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
                    await s_deviceClient.OpenAsync();
                    _logger.LogDebug($"Opened the client instance.");

                    switch (_transportType)
                    {
                        case TransportType.Amqp:
                        case TransportType.Amqp_Tcp_Only:
                        case TransportType.Amqp_WebSocket_Only:
                        case TransportType.Mqtt:
                        case TransportType.Mqtt_Tcp_Only:
                        case TransportType.Mqtt_WebSocket_Only:
                            await s_deviceClient.SetReceiveMessageHandlerAsync(CloudToDeviceMessageHandler, s_deviceClient);
                            _logger.LogInformation("Subscribed to recieve C2D messages," +
                                " use the IoT Hub Azure Portal/ Azure IoT Explorer/ service client SDK to send a message to this device.");
                            break;
                    }

                }
                catch (UnauthorizedException)
                {
                    // Handled by the ConnectionStatusChangeHandler
                }
            }
        }

        // It is not good practice to have async void methods, however, DeviceClient.SetConnectionStatusChangesHandler() event handler signature has a void return type.
        // As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock
        // before attempting to initialize or dispose the device client instance.
        private async void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");
            s_connectionStatus = status;

            switch (status)
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
                            // When getting this reason, the current connection string being used is not valid.
                            // If we had a backup, we can try using that.
                            _deviceConnectionStrings.RemoveAt(0);
                            if (_deviceConnectionStrings.Any())
                            {
                                _logger.LogWarning($"The current connection string is invalid. Trying another.");
                                await InitializeAndOpenClientAsync();
                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndOpenClientAsync();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndOpenClientAsync();
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

        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    _logger.LogInformation($"Device sending message {++messageCount} to IoT Hub...");

                    (Message message, string payload) = PrepareMessage(messageCount);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await s_deviceClient.SendEventAsync(message);
                            _logger.LogInformation($"Sent message {messageCount} of {payload}");
                            message.Dispose();
                            break;
                        }
                        catch (IotHubException ex) when (ex.IsTransient)
                        {
                            _logger.LogError($"A transient IotHubException was caught, but the device application will retry sending the message: {ex}");
                        }
                        catch (UnauthorizedException ex)
                        {
                            _logger.LogError($"An UnauthorizedException was caught, but the recovery will be handled by the ConnectionStatusChangeHandler: {ex}");
                        }
                        catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                        {
                            _logger.LogError($"A network related exception was caught, but the device application will retry sending the message: {ex}");
                        }

                        // wait and retry
                        await Task.Delay(s_sleepDuration);
                    }
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    _logger.LogInformation($"Device waiting for C2D messages from the hub for {s_sleepDuration}...");
                    _logger.LogInformation("Use the IoT Hub Azure Portal or Azure IoT Explorer to send a message to this device.");

                    try
                    {
                        using Message receivedMessage = await s_deviceClient.ReceiveAsync();
                        if (receivedMessage == null)
                        {
                            _logger.LogInformation("No message received; timed out.");
                            await Task.Delay(s_sleepDuration);
                            continue;
                        }

                        await CloudToDeviceMessageHandler(receivedMessage, null);
                    }
                    catch (IotHubException ex) when (ex.IsTransient)
                    {
                        _logger.LogError($"A transient IotHubException was caught, but the device application will retry receiving the message: {ex}");
                    }
                    catch (UnauthorizedException ex)
                    {
                        _logger.LogError($"An UnauthorizedException was caught, but the recovery will be handled by the ConnectionStatusChangeHandler: {ex}");
                    }
                    catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        _logger.LogError($"A network related exception was caught, but the device application will retry receiving the message: {ex}");
                    }
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private Task CloudToDeviceMessageHandler(Message receivedMessage, object context)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            var formattedMessage = new StringBuilder($"Received message: [{messageData}], with lockToken: [{receivedMessage.LockToken}].\n");

            foreach (var prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
            }
            _logger.LogInformation(formattedMessage.ToString());

            _receivedMessagesQueue.Enqueue(receivedMessage);
            return Task.CompletedTask;
        }

        private async Task CompleteReceivedMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    if (_receivedMessagesQueue.TryDequeue(out Message messageToBeCompleted))
                    {
                        try
                        {
                            _receivedMessagesQueue.TryDequeue(out Message messageToBeCompleted11);
                            _logger.LogInformation($"To complete message with lockToken [{messageToBeCompleted11.LockToken}].");
                            await s_deviceClient.CompleteAsync(messageToBeCompleted11);
                            _logger.LogInformation($"Completed message with lockToken [{messageToBeCompleted11.LockToken}].");

                            // There is a single threaded access to this ConcurrentQueue, so we can ensure that the first element that was peeked above is the element that is getting dequeued.
                            _receivedMessagesQueue.TryDequeue(out _);
                            messageToBeCompleted.Dispose();
                        }
                        catch (DeviceMessageLockLostException ex)
                        {
                            _logger.LogWarning($"Attempted to complete a received message whose lock token has expired, the device application will ignore the message as it will need to be received again: {ex}");

                            // There is a single threaded access to this ConcurrentQueue, so we can ensure that the first element that was peeked above is the element that is getting dequeued.
                            _receivedMessagesQueue.TryDequeue(out _);
                            messageToBeCompleted.Dispose();
                        }
                        catch (IotHubException ex) when (ex.IsTransient)
                        {
                            _logger.LogError($"A transient IotHubException was caught, but the device application will retry marking the message as \"Complete\": {ex}");
                        }
                        catch (UnauthorizedException ex)
                        {
                            _logger.LogError($"An UnauthorizedException was caught, but the recovery will be handled by the ConnectionStatusChangeHandler: {ex}");
                        }
                        catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                        {
                            _logger.LogError($"A network related exception was caught, but the device application will retry marking the message as \"Complete\": {ex}");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("There are no C2D messages to be completed.");
                    }
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private (Message, string) PrepareMessage(int messageId)
        {
            var temperature = s_randomGenerator.Next(20, 35);
            var humidity = s_randomGenerator.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = messageId.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };
            eventMessage.Properties.Add("temperatureAlert", (temperature > TemperatureThreshold) ? "true" : "false");

            return (eventMessage, messagePayload);
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private bool ShouldClientBeInitialized(ConnectionStatus connectionStatus)
        {
            return (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Disabled)
                && _deviceConnectionStrings.Any();
        }
    }
}
