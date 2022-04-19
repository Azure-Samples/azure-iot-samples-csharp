# Using Azure IoT Device Provisioning Certificate Signing Requests

- Device Provisioning Service (DPS) can be configured to receive Certificate Signing Requests (CSRs) from IoT devices as a part of the DPS registration process. DPS will then forward the CSR on to the Certificate Authority (CA) linked to your DPS instance. 
- The CA will sign and return an X.509 device identity certificate (aka client certificate) to DPS. We refer to this as an operational certificate. DPS will register the device and operational client certificate thumbprint in IoT hub and return the certificate to the IoT device. The IoT device can then use the operational certificate to authenticate with IoT hub. 
- An onboarding authentication mechanism (either SAS token or X509 client certificate) is still required to authenticate the device with DPS.

## Certificate Signing Request Flow

From [Azure IoT C-SDK](https://github.com/Azure/azure-iot-sdk-c/blob/59d9ae9131fba61a2862b20d332fd0ca99bc8174/provisioning_client/devdoc/using_certificate_signing_requests.md) dev doc:

![dpsCsr](https://www.plantuml.com/plantuml/png/bLDTRzCm57tFhpYFK3K12V50DKthKWO83MrDaP0G3svysrxXsS5sDlBlkQar54AaTM-Ex_auvrgv257vsDwPR4NtN1FoSwJJ0X_8abUHC9kvfZ-niyhCPdXVbg_MrH9DkpLSvutd-nxsaxgyKUmdf4fFWWWeoKVUSTt3AzvRjdkiwLTB8Q8DyZNsEiNZfbfWsSO3sPYKarArhEROt5p3UPI6POflRr-_wntirYcl1IG6jIVTCvK9cKWDKo_BpsCVJtiEaJnUf5DA-adFSUbqj_WlVbcjNqxAfgl1Fle6pdES5ZaIplBJ2Add03fk8Glt7RuNHV5Z7ExGRgzkLr_cqCSBQVOSzOzVBwVUuca98URdHtOIUv81-jsm9rqykf_boVtwThF1YaFjhAKwPjO9sD0kPaYpDz2E0kGUWecABMIajEPa1lsN34ygE5jaPBKGfgkUMd6KSx0beU1AiMaz_HDtS-204Ac1XA4GbE_WhEaWimSX8pcdLfCX7rzrJSV_73lZ7h8B0RPtvWy0 "dpsCsr")

## Public API

```diff
{
     namespace Microsoft.Azure.Devices.Provisioning.Client {
         public class ProvisioningDeviceClient {
             public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default(CancellationToken));
             public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, TimeSpan timeout);
         }
         public class ProvisioningRegistrationAdditionalData {
+          public string OperationalCertificateRequest { get; set; }
         }
         public class DeviceRegistrationResult {
+           public string IssuedClientCertificate { get; set; }
         }
     }
 }
```

## Sample

The `SymmetricKeyWithOperationalCertificateSample` demonstrates how to use the public API to submit a certificate 
signing request then use the provisioned device certificate when connecting to Azure IoT hub.

This sample uses symmetric keys for the onboarding authentication with DPS. You can use either SAS token based (symmetric key or TPM) or X509 certificate based authentication for the onboarding authentication with DPS.

1. Prerequisites

    1. Using curl, submit the following command to the DPS Service API to create a CA object in DPS.

        > **Note:** On Windows, we recommend using curl from the Linux subsystem for Windows.

        For **DigiCert**:
        
        ```bash
        curl -k -L -i -X PUT https://<dps_service_endpoint>/certificateAuthorities/<ca_name>?api-version=2021-11-01-preview -H "Authorization: <service_api_sas_token>" -H "Content-Type: application/json" -H "Content-Encoding: utf-8" -d"{'certificateAuthorityType':'DigiCertCertificateAuthority','apiKey':'<api_key>','profileName':'<profile_id>'}"
        ```

        Where:
        - **<dps_service_endpoint>** - Your DPS Service Endpoint from the DPS Overview blade in Azure portal.
        - **<ca_name>** - The friendly name you wish to assign to your CA. A lower-case string (up to 128 characters long) of alphanumeric characters plus certain special characters : ._ -. No special characters allowed at start or end. 
        - **<service_api_sas_token>** - The DPS Service API shared access token.
        - **<api_key>** - Your DigiCert API key.
        - **<profile_id>** - Your DigiCert **Client Cert Profile ID**.

    1. Create a symmetric key enrollment in DPS and link it to your Certificate Authority.
        Linking your enrollment to the Certificate Authority is currently unavailable through the portal, so you will need to use the following API to create your enrollment and link it to your Certificate Authority.

        The example below is for an `IndividualEnrollment`. `EnrollmentGroup` also supports setting the `ClientCertificateIssuancePolicy` in a similar fashion.

        ```csharp
        IndividualEnrollment individualEnrollment = new IndividualEnrollment(registrationId, attestation))
        {
            ClientCertificateIssuancePolicy = new ClientCertificateIssuancePolicy
            {
                CertificateAuthorityName = caName,
            },
        };

        using var provisioningService = ProvisioningServiceClient.CreateFromConnectionString(provisioningConnectionString);
        IndividualEnrollment createdEnrollment = await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
        ```

        Where:
        - **\<registrationId>** - The RegistrationID of the individual entrollment that you'd like to modify.
        - **\<attestation>** - The attestation details of the individual entrollment that you'd like to modify.
        - **\<caName>** - The ca_name from the previous step.
        - **\<provisioningConnectionString>** - The provisioning service connection string copied over from the DPS shared access policies blade in Azure portal.

1. Run the sample

    1. The sample uses OpenSSL to generate an ECC P-256 public and private key-pair and certificate signing request.
        ```bash
        openssl ecparam -genkey -name prime256v1 -out device1.key
        ```
        ```bash
        openssl req -new -key device1.key -out device1.csr -subj '/CN=myregistration-id'
        ```

        > **Important**: DPS has [character set
    restrictions for registration
    ID](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#registration-id).
        
        > **Note:** The same CSR can be reused and sent to DPS multiple times. You do not have to regenerate the CSR each time. DPS does not impose any restrictions on the cipher suite and key length that can be used. You are free to use RSA or ECC. However, your CA profile must support the cipher suite and key length.

    1. The certificate signing request generated is sent to DPS through the following API
        ```csharp
        public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default);
        ```

        Where:
        ```csharp
        public class ProvisioningRegistrationAdditionalData {
            public string JsonData { get; set; }
            public string OperationalCertificateRequest { get; set; }
        }
        ```

    1. DPS forwards the certificate signing request to your linked Certificate Authority which signs the request and returns the signed operational certificate. 

    1. The IoT hub Device SDK needs both the signed certificate as well as the private key information. It expects to load a single PFX-formatted bundle containing all necessarily information. This sample uses OpenSSL to combine the key and certificate to create the PFX file:
        ```bash
        openssl pkcs12 -export -out device1.pfx -inkey device1.key -in device1.cer
        ```

    1. The authenticated client is used to send a telemetry message to the assigned IoT hub and then close the connection.
