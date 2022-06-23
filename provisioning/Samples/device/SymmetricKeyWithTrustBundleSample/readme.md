# Using Azure IoT Device Provisioning Trust Bundle

- Device Provisioning Service (DPS) can be configured to accept one or more root certificates from your private PKI/CA to it. This collection of root certificates is known as a Trust Bundle.
- When IoT devices and IoT Edge gateways provision using DPS, the Trust Bundle is provided to the device so it can trust the TLS server certificate presented by the upstream Edge gateway (which is chained up to the same root).

## Trust Bundle Flow

From [Azure IoT DPS Trust Bundle](https://github.com/Azure/CertsForIoT-B#trust-bundle) dev doc:

[![](https://mermaid.ink/img/pako:eNp9ks1OwzAQhF9l5VMrQsWFAzlUCkklkAABDRKHXFx7m1pN7OCfoqrqu7MhiVpoIZcks9_Y4_XumDASWcwcfgTUAjPFS8vrQgM9PHijQ71A2_8LbyykwXlTD1rDrVdCNVx7uDc5ZLhRAk9r2fP8VEyT84vMZInnK3dhUeiulCaX0-kQJobMfOrKcAmNVRvuEawxHgSSf6kECb2rN5CXIsXw1nybfsPAHXDILdFwG7Ss_rCnFlsatTVVVSOl5FpCpfQavDlZoVtDG3KYDdqjhsX9G5ynEzsITcceiGHHVyyV82hH444gcSglwa-eDirJxxskzqlSoxz6GP0IF8H75PrqBkSl2lO0rYgAvZicyTFcUQyPL3kOqdEahR-N4QLq_GH-ryPHCmv0dnug2kLPUKwjhEWMWl1zJWlCd62hYH5FtYLF9Cm5XRes0HviQiPpHmZS0YSyeMkrhxFr53e-1YLF3gYcoH7Ee2r_BSmE_mk)](https://mermaid-js.github.io/mermaid-live-editor/edit#pako:eNp9ks1OwzAQhF9l5VMrQsWFAzlUCkklkAABDRKHXFx7m1pN7OCfoqrqu7MhiVpoIZcks9_Y4_XumDASWcwcfgTUAjPFS8vrQgM9PHijQ71A2_8LbyykwXlTD1rDrVdCNVx7uDc5ZLhRAk9r2fP8VEyT84vMZInnK3dhUeiulCaX0-kQJobMfOrKcAmNVRvuEawxHgSSf6kECb2rN5CXIsXw1nybfsPAHXDILdFwG7Ss_rCnFlsatTVVVSOl5FpCpfQavDlZoVtDG3KYDdqjhsX9G5ynEzsITcceiGHHVyyV82hH444gcSglwa-eDirJxxskzqlSoxz6GP0IF8H75PrqBkSl2lO0rYgAvZicyTFcUQyPL3kOqdEahR-N4QLq_GH-ryPHCmv0dnug2kLPUKwjhEWMWl1zJWlCd62hYH5FtYLF9Cm5XRes0HviQiPpHmZS0YSyeMkrhxFr53e-1YLF3gYcoH7Ee2r_BSmE_mk)

## Public API

```diff
{
     namespace Microsoft.Azure.Devices.Provisioning.Client {
         public class ProvisioningDeviceClient {
             public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default(CancellationToken));
             public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, TimeSpan timeout);
         }
         public class DeviceRegistrationResult {
+           public string TrustBundle { get; set; }
         }
+		 public class TrustBundle {
+		 	public TrustBundle();
+		 	public List<X509CertificateWithMetadata> Certificates { get; internal set; }
+		 	public DateTime CreatedDateTime { get; internal set; }
+		 	public string Etag { get; internal set; }
+		 	public string Id { get; internal set; }
+		 	public DateTime LastModifiedDateTime { get; internal set; }
+		 }
	 
+		 public class X509CertificateMetadata {
+		 	public X509CertificateMetadata();
+		 	public string IssuerName { get; internal set; }
+		 	public DateTime NotAfterUtc { get; internal set; }
+		 	public DateTime NotBeforeUtc { get; internal set; }
+		 	public int SerialNumber { get; internal set; }
+		 	public string Sha1Thumbprint { get; internal set; }
+		 	public string Sha256Thumbprint { get; internal set; }
+		 	public string SubjectName { get; internal set; }
+		 }
	 
+		 public class X509CertificateWithMetadata {
+		 	public X509CertificateWithMetadata();
+		 	public string Certificate { get; internal set; }
+		 	public X509CertificateMetadata Metadata { get; internal set; }
+		 }
     }
}
```

## Sample

The `SymmetricKeyWithTrustBundleSample` demonstrates how to use the public API to make a registration request to DPS and receive the trust bundle certificates in return.

1. Prerequisites

    1. Using curl, submit the following command to the DPS Service API to create the Trust Bundle in DPS. This command uploads a test private root certificate to DPS as a trust bundle.

        > **Note:** On Windows, we recommend using curl from the Linux subsystem for Windows.

        ```bash
        curl -k -L -i -X PUT https://<dps_service_endpoint>/trustBundles/<tb_id>?api-version=2021-11-01-preview -H "Authorization: <service_api_sas_token>" -H "Content-Type: application/json" -H "Content-Encoding: utf-8" --data-raw "{'certificates': [{'certificate': '<certificate_as_pem>'}]}"
        ```

        Where:
        - **<dps_service_endpoint>** - Your DPS Service Endpoint from the DPS Overview blade in the Azure portal.
        - **<tb_id>** - The friendly name you wish to assign to your Trust Bundle. A case-insensitive string (up to 128 characters long) of alphanumeric characters plus certain special characters : . _ -. No special characters allowed at start or end.
        - **<service_api_sas_token>** - The DPS Service API shared access token.
        - **<certificate_as_pem>** - The private root certificate (as a pem) from your CA/PKI that will be uploaded to DPS as a Trust Bundle and linked to the enrollment entries.

    1. Create a symmetric key enrollment in DPS and link it to your Trust Bundle.
        Linking your enrollment to the Trust Bundle is currently unavailable through the portal, so you will need to use the following API to create your enrollment and link it to your Trust Bundle.

        The example below is for an `IndividualEnrollment`. `EnrollmentGroup` also supports setting the `TrustBundleId` in a similar fashion.

        ```csharp
        IndividualEnrollment individualEnrollment = new IndividualEnrollment(registrationId, attestation)
        {
            TrustBundleId = tbId,
        };

        using var provisioningService = ProvisioningServiceClient.CreateFromConnectionString(provisioningConnectionString);
        IndividualEnrollment createdEnrollment = await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
        ```

        Where:
        - **\<registrationId>** - The RegistrationID of the individual entrollment that you'd like to create or update.
        - **\<attestation>** - The attestation details of the individual entrollment that you'd like to create or update.
        - **\<tbId>** - The tb_id from the previous step.
        - **\<provisioningConnectionString>** - The provisioning service connection string copied over from the DPS shared access policies blade in Azure portal.

1. Run the sample

    1. The sample provisions a symmetric key authneticated individual enrollment that has been linked to the trust bundle during enrollment creation. On DPS registration, this enrollment receives a `TrustBundle` that contains the collection of trusted certificates that have been uploadeded to DPS.

         ```csharp
        public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default);
        ```

        Where:
        ```csharp
        public class DeviceRegistrationResult {
            ...
            public TrustBundle TrustBundle { get; set; }
        }
        ```

    1. The IoT device is required to extract the private root certificate information and add it to its root certificate store (e.g. a cert store, in memory, a file on disk, etc.). This will enable the device SDK find the root certificate during `mTLS` negotation with `$edgeHub`. 

        `mTLS` or mutual TLS is a method for mutual authentication where each party in a client-server communication is required to prove their identity. Each party presents their certificates and authenticates using their public/private key pair.

        During the `mTLS` negotiation with IoT Edge, `$edgeHub` presents its TLS server certificate to the IoT device. This TLS server certificate is chained up to the private root certificate that has been uploaded as a aprt of the Trust Bundle. When the IoT Device receives the `$edgeHub` TLS server certificate, it checks its root certificate store to determine if it trusts the same private root certificate. This is successful because the private root certificate was added to the certificate store above.

        This step to add the Trust Bundle certificates to the certificate store has not been implemented in the sample since each user might have different limitations on installing root certificates to their certificate stoer. We have added a sample implementation below, which can be modified based on your scenario.

        For example, on Windows:

        ```csharp
        DeviceRegistrationResult registrationResult = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
        string trustBundlePem = registrationResult.TrustBundle.Certificates[0].Certificate;

        var certificateStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        certificateStore.Open(OpenFlags.ReadWrite);
        byte[] bytes = Encoding.UTF8.GetBytes(trustBundlePem);
        using var trustedRootCertificate = new X509Certificate2(bytes);
        certificateStore.Add(trustedRootCertificate);
        certificateStore.Close();
        ```

    1. The authenticated IoT device can then be used to send a telemetry message (or perform other operations) to the assigned IoT hub via EdgeHub.
