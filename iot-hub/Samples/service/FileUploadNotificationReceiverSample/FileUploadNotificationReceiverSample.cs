// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    public class FileUploadNotificationReceiverSample
    {
        private readonly string _iotHubConnectionString;
        private readonly ILogger _logger;
        private readonly TransportType _transportType;
        private static ServiceClient _serviceClient;
        private static readonly TimeSpan s_notificationReceiverTimeout = TimeSpan.FromSeconds(5);

        public FileUploadNotificationReceiverSample(string iotHubConnectionString, TransportType transportType, ILogger logger)
        {
            _iotHubConnectionString = iotHubConnectionString ?? throw new ArgumentNullException(nameof(iotHubConnectionString));
            _transportType = transportType;
            _logger = logger;
        }

        public async Task RunSampleAsync(TimeSpan runningTime)
        {
            using var cts = new CancellationTokenSource(runningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            try
            {
                await InitializeServiceClientAsync();
                await ReceiveFileUploadNotifications(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, exiting...: \n{ex}");
            }
        }

        private async Task ReceiveFileUploadNotifications(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Listening for file upload notifications from the service.");
            FileNotificationReceiver<FileNotification> notificationReceiver = _serviceClient.GetFileNotificationReceiver();
            int totalNotificationsReceived = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    FileNotification fileUploadNotification = await notificationReceiver.ReceiveAsync(s_notificationReceiverTimeout);

                    if (fileUploadNotification == null)
                    {
                        _logger.LogInformation($"Did not receive any notification after {s_notificationReceiverTimeout.TotalSeconds} seconds.");
                        continue;
                    }

                    totalNotificationsReceived++;

                    _logger.LogInformation($"Received file upload notification.");
                    _logger.LogInformation($"\tDeviceId: {fileUploadNotification.DeviceId ?? "N/A"}.");
                    _logger.LogInformation($"\tFileName: {fileUploadNotification.BlobName ?? "N/A"}.");
                    _logger.LogInformation($"\tEnqueueTimeUTC: {fileUploadNotification.EnqueuedTimeUtc}.");
                    _logger.LogInformation($"\tBlobSizeInBytes: {fileUploadNotification.BlobSizeInBytes}.");

                    _logger.LogInformation($"Marking notification for {fileUploadNotification.DeviceId} as complete.");

                    await notificationReceiver.CompleteAsync(fileUploadNotification);

                    _logger.LogInformation($"Successfully marked the notification for device {fileUploadNotification.DeviceId} as completed.");
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Caught an exception: {e.Message} - {e}");
                }
            }

            _logger.LogInformation($"Total Notifications Received: {totalNotificationsReceived}.");
            _logger.LogInformation($"Closing the service client.");
            await _serviceClient.CloseAsync();
        }

        private async Task InitializeServiceClientAsync()
        {
            if (_serviceClient != null)
            {
                await _serviceClient.CloseAsync();
                _serviceClient.Dispose();
                _serviceClient = null;
                _logger.LogInformation("Closed and disposed the current service client instance.");
            }

            var options = new ServiceClientOptions
            {
                SdkAssignsMessageId = Shared.SdkAssignsMessageId.WhenUnset,
            };
            _serviceClient = ServiceClient.CreateFromConnectionString(_iotHubConnectionString, _transportType, options);
            _logger.LogInformation("Initialized a new service client instance.");
        }
    }
}
