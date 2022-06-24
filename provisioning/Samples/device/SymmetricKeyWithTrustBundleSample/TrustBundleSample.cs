// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service and receive the Trust Bundle information in the response.
    /// </summary>
    internal class TrustBundleSample
    {
        private readonly Parameters _parameters;
        private readonly ILogger _logger;

        public TrustBundleSample(Parameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public async Task RunSampleAsync()
        {
            _logger.LogInformation($"Initializing the device provisioning client...");

            // You will need to use a symmetric key enrollment that has been linked to your Trust Bundle.
            // The linking can be performed by setting the TrustBundleId when creating the enrollment entry.
            using var security = new SecurityProviderSymmetricKey(
                _parameters.Id,
                _parameters.PrimaryKey,
                null);

            using var transportHandler = GetTransportHandler();

            // Pass in your onboarding credentials when creating your provisioning device client instance.
            // This credential will be used to authenticate your request with the device provisioning service (DPS).
            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                _parameters.GlobalDeviceEndpoint,
                _parameters.IdScope,
                security,
                transportHandler);

            _logger.LogInformation($"Initialized for registration Id {security.GetRegistrationID()}.");

            _logger.LogInformation("Registering with the device provisioning service...");
            DeviceRegistrationResult result = await provClient.RegisterAsync();

            _logger.LogInformation($"Registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                _logger.LogError($"Registration status did not assign a hub, so exiting this sample. You will nede to inspect the registration result and take appropriate next steps.");
                _logger.LogError($"Error code: {result.ErrorCode}, error message: {result.ErrorMessage}.");
                return;
            }

            _logger.LogInformation($"Device {result.DeviceId} registered to {result.AssignedHub}.");

            if (result.TrustBundle == null || !result.TrustBundle.Certificates.Any())
            {
                _logger.LogError($"Expected trust bundle was not returned by DPS, so exiting this sample.");
                return;
            }

            _logger.LogInformation($"The following {result.TrustBundle.Certificates.Count} certificate(s) have been uploaded as trusted root certificates.");
            _logger.LogInformation("You will need to add these certificates to your device's certificate store so that it can successfully negotiate mTLS authentication with $edgeHub.");

            int certificateCount = 0;
            foreach(var certificate in result.TrustBundle.Certificates)
            {
                string certificatePem = certificate.Certificate;
                byte[] certificateBytes = Encoding.UTF8.GetBytes(certificatePem);
                using var x509Certificate = new X509Certificate2(certificateBytes);

                _logger.LogInformation($"Information for certificate {++certificateCount}.");
                PrintCertificateInformation(x509Certificate);
            }

            _logger.LogInformation("Done.");
        }

        private void PrintCertificateInformation(X509Certificate2 certificate)
        {
            _logger.LogInformation($"Subject: {certificate.Subject}");
            _logger.LogInformation($"Issuer: {certificate.IssuerName.Name}");
            _logger.LogInformation($"SHA1: {certificate.GetCertHashString()}");
            _logger.LogInformation($"SHA256: {certificate.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256)}");
            _logger.LogInformation($"Serial: {certificate.SerialNumber}");
        }

        private ProvisioningTransportHandler GetTransportHandler()
        {
            return _parameters.TransportType switch
            {
                TransportType.Mqtt => new ProvisioningTransportHandlerMqtt(),
                TransportType.Mqtt_Tcp_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly),
                TransportType.Mqtt_WebSocket_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly),
                TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
                TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
                TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
                TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
                _ => throw new NotSupportedException($"Unsupported transport type {_parameters.TransportType}"),
            };
        }
    }
}
