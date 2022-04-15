// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a symmetric key, and then
    /// use the registration information to authenticate to IoT hub.
    /// </summary>
    internal class ProvisioningDeviceClientSample
    {
        private readonly Parameters _parameters;
        private readonly ILogger _logger;
        private readonly DirectoryInfo s_dpsClientCertificateFolder;

        public ProvisioningDeviceClientSample(Parameters parameters, ILogger logger)
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
                // Generate the certificate signing request for requesting X509 onboarding certificate for authenticating with IoT hub.
                // This sample uses openssl to generate an ECC P-256 public-private key-pair and the corresponding certificate signing request.
                string certificateSigningRequest = GenerateClientCertKeyPairAndCsr(_parameters.Id);

                _logger.LogInformation($"Initializing the device provisioning client...");

                // For individual enrollments, the first parameter must be the registration Id, where in the enrollment
                // the device Id is already chosen. However, for group enrollments the device Id can be requested by
                // the device, as long as the key has been computed using that value.
                // Also, the secondary key could be included, but was left out for the simplicity of this sample.
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

                // Pass in your certificate signing request along with the registration request.
                // DPS will forward your signing request to your linked certificate authority (CA).
                // The CA will sign and return an operational X509 device identity certificate (aka client certificate) to DPS.
                // DPS will register the device and operational client certificate thumbprint in IoT hub and return the certificate to the IoT device.
                // The IoT device can then use the operational certificate to authenticate with IoT hub.

                var registrationData = new ProvisioningRegistrationAdditionalData
                {
                    OperationalCertificateRequest = certificateSigningRequest,
                };

                _logger.LogInformation("Registering with the device provisioning service...");
                DeviceRegistrationResult result = await provClient.RegisterAsync(registrationData);

                _logger.LogInformation($"Registration status: {result.Status}.");
                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    _logger.LogError($"Registration status did not assign a hub, so exiting this sample.");
                    return;
                }

                _logger.LogInformation($"Device {result.DeviceId} registered to {result.AssignedHub}.");

                if (result.IssuedClientCertificate == null)
                {
                    _logger.LogError($"Expected operational certificate was not returned by DPS, so exiting this sample.");
                    return;
                }

                // This sample uses openssl to generate the pfx certificate from the issued operational certificate and the previously created ECC P-256 public-private key-pair.
                // This certificate will be used when authneticating with IoT hub.
                _logger.LogInformation("Creating an X509 certificate from the issued operational certificate...");
                using X509Certificate2 clientCertificate = GenerateOperationalCertificateFromIssuedCertificate(result.RegistrationId, result.IssuedClientCertificate);
                IAuthenticationMethod auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, clientCertificate);

                _logger.LogInformation($"Testing the provisioned device with IoT hub...");
                using DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, _parameters.TransportType);

                _logger.LogInformation("Sending a telemetry message...");
                using var message = new Message(Encoding.UTF8.GetBytes("TestMessage"));
                await iotClient.SendEventAsync(message);
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

        private string GenerateClientCertKeyPairAndCsr(string registrationId)
        {
            // Generate EC public-private key-pair
            _logger.LogInformation($"Generating ECC P-256 {registrationId}.key file using ...");
            string keyGen = $"ecparam -genkey -name prime256v1 -out {s_dpsClientCertificateFolder}\\{registrationId}.key";
            _logger.LogInformation($"Running: openssl {keyGen}");

            using var keyGenProcess = Process.Start("openssl", keyGen);
            keyGenProcess.WaitForExit();

            // Generate the certificate signing request
            _logger.LogInformation($"Generating {registrationId}.csr file using ...");
            string csrGen = $"req -new -key {s_dpsClientCertificateFolder}\\{registrationId}.key -out {s_dpsClientCertificateFolder}\\{registrationId}.csr -subj /CN={registrationId}";
            _logger.LogInformation($"Running: openssl {csrGen}");
            
            using var csrGenProcess = Process.Start("openssl", csrGen);
            csrGenProcess.WaitForExit();

            return File.ReadAllText($"{s_dpsClientCertificateFolder}\\{registrationId}.csr");
        }

        private X509Certificate2 GenerateOperationalCertificateFromIssuedCertificate(string registrationId, string issuedCertificate)
        {
            // Write the issued public certificate to disk
            File.WriteAllText($"{s_dpsClientCertificateFolder}\\{registrationId}.cer", issuedCertificate);

            // Generate the pfx file containing the public certificate information returned by DPS and the private certificate information genertated previously using openssl ecparam command.
            _logger.LogInformation($"Generating {registrationId}.pfx file using ...");
            string pfxGen = $"pkcs12 -export -out {s_dpsClientCertificateFolder}\\{registrationId}.pfx -inkey {s_dpsClientCertificateFolder}\\{registrationId}.key -in {s_dpsClientCertificateFolder}\\{registrationId}.cer -passout pass:";
            _logger.LogInformation($"Running: openssl {pfxGen}");
            
            using var pfxGenProcess = Process.Start("openssl", pfxGen);
            pfxGenProcess.WaitForExit();

            return new X509Certificate2($"{s_dpsClientCertificateFolder}\\{registrationId}.pfx");
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
