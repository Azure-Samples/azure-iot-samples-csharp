# Using Azure IoT Device Provisioning Certificate Signing Requests

- Device Provisioning Service (DPS) can be configured to receive Certificate Signing Requests (CSRs) from IoT devices as a part of the DPS registration process. DPS will then forward the CSR on to the Certificate Authority (CA) linked to your DPS instance. 
- The CA will sign and return an X.509 device identity certificate (aka client certificate) to DPS. We refer to this as an operational certificate. DPS will register the device and operational client certificate thumbprint in IoT Hub and return the certificate to the IoT device. The IoT device can then use the operational certificate to authenticate with IoT Hub. 
- An onboarding authentication mechanism (either SAS token or X509 Client 
Certificate) is still required to authenticate the device with DPS.

## Certificate Signing Request Flow

![dpsCSR](https://www.plantuml.com/plantuml/png/bPFVJy8m4CVVzrVSeoumJOmF4aCOGzGO331CJ8mFPJsWSRQpxKJ-Uwyh69nBmBV-kC_tldVNzenbsfRlEV329Eaq6E2do13QNV2h3joYHCqiGXYgmgs4aYmFGxX9ahDf6iCRRje54xg1JJGIQI11RSL2P4uc5Kifv1Ac-56YiL0QjwkBDucEqmx4fLsXj5xAescSjc0s7e7IaEI2Rk7vylpAISgvOffJ42bc6haZMMu2ajgt6ISFzJmQby9Or73YLzxQFMz1N_5DvuzVwjrfewm_sck0gq1fOPj5Ak2wVIHGrRaNMg-2Egmty195qMlTtAgS3oU3nnRmwi1LzW_rkwT-uomEIX3OxbRqLkmG0VXL21fTjCjEpQduqMGsWu4mcP8ICnj8HS4vBcm0_ku2kAAtH-T0CPO92NJ5E1S-6V0VcCRDZ99HW98xeB7KOqki-Tph4Y4mP28lDVwoEri90_JQ2Y0pQ0oZeIcPRvpVDS7RpBwgHfExgKwn-j2moDKw27eKIN_x6m00 "dpsCSR")

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
signing request then use the provisioned device certificate when connecting to Azure IoT Hub.

This sample uses symmetric keys for the onboarding authentication with DPS. You can use either SAS token based (symmetric key or TPM) or X509 certificate based authentication for the onboarding authentication with DPS.

1. Prerequisites

    1. Using curl, submit the following command to the DPS Service API to create a CA object in DPS.

        **Note:** On Windows, we recommend using curl from the Linux subsystem for Windows.

        For **DigiCert**:
        
        ```bash
        curl -k -L -i -X PUT https://<dps_service_endpoint>/certificateAuthorities/<ca_name>?api-version=2021-11-01-preview -H "Authorization: <service_api_sas_token>" -H "Content-Type: application/json" -H "Content-Encoding: utf-8" -d"{'certificateAuthorityType':'DigiCertCertificateAuthority','apiKey':'<api_key>','profileName':'<profile_id>'}"
        ```

        Where:
        - **<dps_service_endpoint>** - Your DPS Service Endpoint from the DPS Overview blade in Azure.
        - **<ca_name>** - The friendly name you wish to assign to your CA. A lower-case string (up to 128 characters long) of alphanumeric characters plus certain special characters : ._ -. No special characters allowed at start or end. 
        - **<service_api_sas_token>** - The DPS Service API shared access token.
        - **<api_key>** - Your DigiCert API key.
        - **<profile_id>** - Your DigiCert **Client Cert Profile ID**.

    1. Create a symmetric key enrollment in DPS and link it to your Certificate Authority.
        Linking your enrollment to the Certificate Authority is currently unavailable through the portal, so you will need to run the following curl commands.

        1. Query the Service API for your individual enrollment:

            ```bash
            curl -X GET -H "Content-Type: application/json" -H "Content-Encoding:  utf-8" -H "Authorization: <service_api_sas_token>" https://<dps_service_endpoint>/enrollments/<registration_id>?api-version=2021-11-01-preview > enrollment.json
            ```

            Where:
            - **<dps_service_endpoint>** - Your DPS Service Endpoint from the DPS Overview blade in Azure.
            - **<service_api_sas_token>** - The DPS Service API shared access token.
            - **<registration_id>** - The RegistrationID of the individual entrollment that you'd like to modify.

        1.  Update the individual enrollment as follows, using the above JSON. Edit the `enrollment.json` file to add the following information:
            ```json
            "clientCertificateIssuancePolicy": {
                "certificateAuthorityName": "ca1"
            }
            ```
            
            Replace ca1 with **<ca_name>**.

        1. Update the enrollment information:

            ```bash
            curl -k -L -i -X PUT -H "Content-Type: application/json" -H "Content-Encoding:  utf-8" -H "Authorization: <service_api_sas_token>" https://<dps_service_endpoint>/enrollments/<registration_id>?api-version=2021-11-01-preview -H "If-Match: <etag>" -d @enrollment.json
            ```

            Where:
            - **<dps_service_endpoint>** - Your DPS Service Endpoint from the DPS Overview blade in Azure.
            - **<registration_id>** – The RegistrationID of the individual entrollment that you'd like to modify.
            - **<service_api_sas_token>** - The DPS Service API shared access token.
            - **<etag>** - The etag found in enrollment.json 

1. Run the sample

    1. The sample uses OpenSSL to generate an ECC P-256 keypair and certificate signing request.
        ```bash
        openssl ecparam -genkey -name prime256v1 -out device1.key
        ```
        ```bash
        openssl req -new -key device1.key -out device1.csr -subj '/CN=myregistration-id'
        ```

        **Important**: DPS has [character set
    restrictions for registration
    ID](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#registration-id).
        
        **Note:** The same CSR can be reused and sent to DPS multiple times. You do not have to regenerate the CSR each time. DPS does not impose any restrictions on the cipher suite and key length that can be used. You are free to use RSA or ECC. However, your CA profile must support the cipher suite and key length.

    1. The certificate signing request generated is sent to DPS through the following API
        ```c#
        public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default);
        ```

    1. DPS forwards the certificate signing request to your linked Certificate Authority which signs the request and returns the signed operational certificate. 

    1. The IoT Hub Device SDK needs both the signed certificate as well as the private key information. It expects to load a single PFX-formatted bundle containing all necessarily information. This sample uses OpenSSL to combine the key and certificate to create the PFX file:
        ```bash
        openssl pkcs12 -export -out device1.pfx -inkey device1.key -in device1.cer
        ```

    1. The authenticated client is used to send a telemetry message to the assigned IoT hub and then close the connection.
