﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
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
        private static readonly TransportType[] _amqpTransports = new[] { TransportType.Amqp, TransportType.Amqp_Tcp_Only, TransportType.Amqp_WebSocket_Only };
        private static readonly Random s_randomGenerator = new Random();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private readonly List<string> _deviceConnectionStrings;
        private readonly TransportType _transportType;
        private readonly ClientOptions _clientOptions = new ClientOptions { SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset };

        private readonly ILogger _logger;

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
                await InitializeAndSetupClientAsync(cts.Token);
                await Task.WhenAll(SendMessagesAsync(cts.Token), ReceiveMessagesAsync(cts.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }

            _initSemaphore.Dispose();
        }

        private async Task InitializeAndSetupClientAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldClientBeInitialized(s_connectionStatus))
            {
                // Allow a single thread to dispose and initialize the client instance.
                await _initSemaphore.WaitAsync();
                try
                {
                    if (ShouldClientBeInitialized(s_connectionStatus))
                    {
                        _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                        // If the device client instance has been previously initialized, then dispose it.
                        if (s_deviceClient != null)
                        {
                            await s_deviceClient.CloseAsync();
                            s_deviceClient.Dispose();
                            s_deviceClient = null;
                        }
                    }

                    s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionStrings.First(), _transportType, _clientOptions);
                    s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                    _logger.LogDebug("Initialized the client instance.");
                }
                finally
                {
                    _initSemaphore.Release();
                }

                try
                {
                    // Force connection now.
                    // Client operations have implicit open enabled.
                    await SubscribeToTwinUpdateNotificationsAsync(cancellationToken);
                    _logger.LogDebug("Opened the client connection and subscribed to desired property update notifications");
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
            _logger.LogInformation($"Connection status changed: status={status}, reason={reason}");
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
                                await InitializeAndSetupClientAsync();
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

                            await InitializeAndSetupClientAsync();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await InitializeAndSetupClientAsync();
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

        private async Task SubscribeToTwinUpdateNotificationsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleTwinUpdateNotificationsAsync, cancellationToken);
                    break;
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private async Task HandleTwinUpdateNotificationsAsync(TwinCollection twinUpdateRequest, object userContext)
        {
            CancellationToken cancellationToken = (CancellationToken)userContext;

            if (!cancellationToken.IsCancellationRequested)
            {
                var reportedProperties = new TwinCollection();

                _logger.LogInformation($"Twin property update requested: \n{twinUpdateRequest.ToJson()}");

                // For the purpose of this sample, we'll blindly accept all twin property write requests.
                foreach (KeyValuePair<string, object> desiredProperty in twinUpdateRequest)
                {
                    _logger.LogInformation($"Setting property {desiredProperty.Key} to {desiredProperty.Value}.");
                    reportedProperties[desiredProperty.Key] = desiredProperty.Value;
                }

                await RetryOperationHelper.RetryTransientExceptionsAsync(
                        async () =>
                        {
                            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
                        },
                        () => IsDeviceConnected,
                        _logger);
            }
        }

        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    _logger.LogInformation($"Device sending message {++messageCount} to IoT hub...");

                    using Message message = PrepareMessage(messageCount);
                    await RetryOperationHelper.RetryTransientExceptionsAsync(
                        async () =>
                        {
                            await s_deviceClient.SendEventAsync(message);
                        },
                        () => IsDeviceConnected,
                        _logger);
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsDeviceConnected)
                {
                    await Task.Delay(s_sleepDuration);
                    continue;
                }
                else if (_transportType == TransportType.Http1)
                {
                    // The call to ReceiveAsync over HTTP completes immediately, rather than waiting up to the specified
                    // time or when a cancellation token is signaled, so if we want it to poll at the same rate, we need
                    // to add an explicit delay here.
                    await Task.Delay(s_sleepDuration);
                }

                _logger.LogInformation($"Device waiting for C2D messages from the hub for {s_sleepDuration}." +
                    $"\nUse the IoT Hub Azure Portal or Azure IoT Explorer to send a message to this device.");

                await RetryOperationHelper.RetryTransientExceptionsAsync(
                        async () =>
                        {
                            await ReceiveMessageAndCompleteAsync();
                        },
                        () => IsDeviceConnected,
                        _logger,
                        new Dictionary<Type, string> { { typeof(DeviceMessageLockLostException), "Attempted to complete a received message whose lock token has expired" } });
            }
        }

        private async Task ReceiveMessageAndCompleteAsync()
        {
            using var cts = new CancellationTokenSource(s_sleepDuration);
            Message receivedMessage = null;
            try
            {
                // AMQP library does not take a cancellation token but does take a time span
                // so we'll call this API differently.
                if (_amqpTransports.Contains(_transportType))
                {
                    receivedMessage = await s_deviceClient.ReceiveAsync(s_sleepDuration);
                }
                else
                {
                    receivedMessage = await s_deviceClient.ReceiveAsync(cts.Token);
                }
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                _logger.LogInformation("Timed out waiting to receive a message.");
            }

            if (receivedMessage == null)
            {
                _logger.LogInformation("No message received.");
                return;
            }

            try
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                var formattedMessage = new StringBuilder($"Received message: [{messageData}]");

                foreach (var prop in receivedMessage.Properties)
                {
                    formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
                }
                _logger.LogInformation(formattedMessage.ToString());

                await s_deviceClient.CompleteAsync(receivedMessage);
                _logger.LogInformation($"Completed message [{messageData}].");
            }
            finally
            {
                receivedMessage.Dispose();
            }
        }

        private Message PrepareMessage(int messageId)
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

            return eventMessage;
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
