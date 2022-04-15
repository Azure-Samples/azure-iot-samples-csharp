// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class PnpUsabilitySample
    {
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly string _deviceConnectionString;
        private readonly TransportType _transportType;
        private readonly ClientOptions _clientOptions = new() { SdkAssignsMessageId = Shared.SdkAssignsMessageId.WhenUnset };

        private readonly ILogger _logger;

        // Mark these fields as volatile so that their latest values are referenced.
        private static volatile DeviceClient? s_deviceClient;
        private static volatile ConnectionStatus s_connectionStatus = ConnectionStatus.Disconnected;

        public PnpUsabilitySample(string deviceConnectionString, TransportType transportType, ILogger logger)
        {
            _logger = logger;
            _deviceConnectionString = deviceConnectionString;

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
                await PerformOperationsAsync(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }

            _initSemaphore.Dispose();
        }

        private async Task InitializeAndOpenClientAsync()
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

                    s_deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType, _clientOptions);
                    s_deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                    _logger.LogDebug($"Initialized the client instance.");
                }
                finally
                {
                    _initSemaphore.Release();
                }

                try
                {
                    // Force connection now.
                    // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
                    await s_deviceClient.OpenAsync();
                    _logger.LogDebug($"Opened the client instance.");
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

        private async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsDeviceConnected)
                {
                    while (true)
                    {
                        try
                        {
                        }
                        catch (IotHubException ex) when (ex.IsTransient)
                        {
                            // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                            _logger.LogError($"An IotHubException was caught, but will try to recover and retry: {ex}");
                        }
                        catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                        {
                            _logger.LogError($"A network related exception was caught, but will try to recover and retry: {ex}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Unexpected error {ex}");
                        }

                        // wait and retry
                        await Task.Delay(s_sleepDuration);
                    }
                }

                await Task.Delay(s_sleepDuration);
            }
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private static bool ShouldClientBeInitialized(ConnectionStatus connectionStatus)
        {
            return (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Disabled);
        }
    }
}
