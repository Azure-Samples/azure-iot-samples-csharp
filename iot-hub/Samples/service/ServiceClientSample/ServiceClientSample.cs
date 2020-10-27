// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ServiceClientSample
    {
        private readonly ServiceClient _serviceClient;
        private readonly string _deviceId;
        private readonly ILogger _logger;

        public ServiceClientSample(ServiceClient serviceClient, string deviceId, ILogger logger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _logger = logger;
        }

        public async Task RunSampleAsync()
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            try
            {
                await SendC2DMessagesAsync(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }

        }

        private async Task SendC2DMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var str = $"Hello, Cloud! - Message {++messageCount }";
                var message = new Message(Encoding.ASCII.GetBytes(str));
                _logger.LogInformation($"\tSending C2D message {messageCount} with Id {message.MessageId} to {_deviceId} . . . ");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await _serviceClient.SendAsync(_deviceId, message, TimeSpan.FromSeconds(10));
                        _logger.LogInformation($"Sent message {messageCount}.");
                        message.Dispose();
                        break;
                    }
                    catch (Exception e) when (ExceptionHelper.IsNetwork(e))
                    {
                        _logger.LogError("Transient Exception occurred, will retry...");
                        await Task.Delay(5 * 1000);
                    }

                }
                await Task.Delay(5 * 1000);
            }
        }
    }
}
