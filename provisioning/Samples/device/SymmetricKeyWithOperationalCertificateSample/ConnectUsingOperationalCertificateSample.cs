// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a symmetric key as
    /// onboarding authentication mechanism and passing in a certificate signing request, and then use the 
    /// issued device certificate to authenticate to IoT hub.
    /// </summary>
    internal class ConnectUsingOperationalCertificateSample
    {
        private readonly Parameters _parameters;
        private readonly ILogger _logger;
        private readonly DirectoryInfo s_dpsClientCertificateFolder;

        public ConnectUsingOperationalCertificateSample(Parameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;

            // Create a folder to hold the DPS client certificates. If a folder by the same name already exists, it will be used.
            s_dpsClientCertificateFolder = Directory.CreateDirectory("DpsClientCertificates");
        }

        public async Task RunSampleAsync()
        {
            try
            {
                _logger.LogInformation($"Initializing the device provisioning client...");

                // You will need to use a symmetric key enrollment that has been linked to your Certificate Authority.
                // The linking can be performed by setting the ClientCertificateIssuancePolicy when creating the enrollment entry.
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

                // Generate the certificate signing request for requesting X509 onboarding certificate for authenticating with IoT hub.
                // This sample uses openssl to generate an ECC P-256 public-private key pair and the corresponding certificate signing request.
                X509Certificate2Helper.GenerateCertificateSigningRequestFiles(security.GetRegistrationID(), s_dpsClientCertificateFolder, _logger);
                string csrFile = Path.Combine(s_dpsClientCertificateFolder.FullName, $"{security.GetRegistrationID()}.csr");
                string certificateSigningRequest = File.ReadAllText(csrFile);

                // Pass in your certificate signing request along with the registration request.
                // DPS will forward your signing request to your linked certificate authority (CA).
                // The CA will sign and return an operational X509 device identity certificate (aka client certificate) to DPS.
                // DPS will register the device and client certificate thumbprint in IoT hub and return the certificate with the public key to the IoT device.
                // The IoT device can then use this returned client certificate along with the private key information to authenticate with IoT hub.

                var registrationData = new ProvisioningRegistrationAdditionalData
                {
                    ClientCertificateSigningRequest = certificateSigningRequest,
                };

                _logger.LogInformation("Registering with the device provisioning service...");
                DeviceRegistrationResult result = await provClient.RegisterAsync(registrationData);

                _logger.LogInformation($"Registration status: {result.Status}.");
                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    _logger.LogError($"Registration status did not assign a hub, so exiting this sample. You will nede to inspect the registration result and take appropriate next steps.");
                    _logger.LogError($"Error code: {result.ErrorCode}, error message: {result.ErrorMessage}.");
                    return;
                }

                _logger.LogInformation($"Device {result.DeviceId} registered to {result.AssignedHub}.");

                if (result.IssuedClientCertificate == null)
                {
                    _logger.LogError($"Expected client certificate was not returned by DPS, so exiting this sample.");
                    return;
                }

                // Write the issued certificate to disk
                string cerFile = Path.Combine(s_dpsClientCertificateFolder.FullName, $"{result.RegistrationId}.cer");
                File.WriteAllText(cerFile, result.IssuedClientCertificate);

                // This sample uses openssl to generate the pfx certificate from the issued client certificate and the previously created ECC P-256 private key.
                // This certificate will be used when authenticating with IoT hub.
                _logger.LogInformation("Creating an X509 certificate from the issued client certificate...");
                X509Certificate2Helper.GeneratePfxFromPublicCertificateAndPrivateKey(result.RegistrationId, s_dpsClientCertificateFolder, _logger);
                using X509Certificate2 clientCertificate = X509Certificate2Helper.CreateX509Certificate2FromPfxFile(result.RegistrationId, s_dpsClientCertificateFolder);

                using var auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, clientCertificate);

                _logger.LogInformation($"Testing the provisioned device with IoT hub...");

                var clientOptions = new ClientOptions
                {
                    SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                };
                using DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, _parameters.TransportType, clientOptions);

                _logger.LogInformation("Sending a telemetry message...");
                using var message = new Message(Encoding.UTF8.GetBytes("TestMessage"));
                await iotClient.SendEventAsync(message);
                _logger.LogInformation($"Sent message with Id {message.MessageId}.");

                await iotClient.CloseAsync();
            }
            finally
            {
                CleanupCertificates();

                _logger.LogInformation("Finished.");
            }
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

        private void CleanupCertificates()
        {
            // Delete all the client certificates created for the sample
            try
            {
                s_dpsClientCertificateFolder.Delete(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Exception occured while deleting client certificates: {ex.Message}");
            }
        }
    }
}