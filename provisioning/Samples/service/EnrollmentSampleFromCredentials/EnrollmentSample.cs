// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Service.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    using DeviceCapabilities = Microsoft.Azure.Devices.Provisioning.Service.Models.DeviceCapabilities;
    public class EnrollmentSample
    {
        private const string RegistrationId = "myvalid-registratioid-csharp";
        private const string TpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

        // Optional parameters
        private const string OptionalDeviceId = "iothubtpmdevice1";
        private const string OptionalProvisioningStatus = "enabled";
        private DeviceCapabilities OptionalEdgeCapabilityEnabled = new DeviceCapabilities {IotEdge = true };
        private DeviceCapabilities OptionalEdgeCapabilityDisabled = new DeviceCapabilities { IotEdge = false };

		private readonly string TpmAttestationType = "tpm";
		private const string IotHubHostName = "my-iothub-hostname";
        ProvisioningServiceClient _provisioningServiceClient;

        public EnrollmentSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
        }

        public async Task RunSampleAsync()
        {
            await QueryIndividualEnrollmentsAsync().ConfigureAwait(false);

            await CreateIndividualEnrollmentTpmAsync().ConfigureAwait(false);
            await UpdateIndividualEnrollmentAsync().ConfigureAwait(false);
            await DeleteIndividualEnrollmentAsync().ConfigureAwait(false);            
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a query for enrollments...");
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");

            IList<IndividualEnrollment> queryResult = await _provisioningServiceClient.QueryIndividualEnrollmentsAsync(querySpecification).ConfigureAwait(false);
            foreach (IndividualEnrollment individualEnrollment in queryResult)
            {
                Console.WriteLine(JsonConvert.SerializeObject(individualEnrollment, Formatting.Indented));
            }
        }

        public async Task CreateIndividualEnrollmentTpmAsync()
        {
            Console.WriteLine("\nCreating a new individualEnrollment...");
            TpmAttestation attestation = new TpmAttestation(TpmEndorsementKey);
            AttestationMechanism attestationMechanism = new AttestationMechanism(TpmAttestationType, attestation);
            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            RegistrationId,
                            attestationMechanism);

            // The following parameters are optional:
            individualEnrollment.DeviceId = OptionalDeviceId;
            individualEnrollment.ProvisioningStatus = OptionalProvisioningStatus;
            IDictionary<string, object> pros = new Dictionary<string, object>() { { "Brand", "Contoso"} };
            individualEnrollment.InitialTwin = new InitialTwin(
                null,
                new InitialTwinProperties(
                    new Models.TwinCollection(
                        new Dictionary<string, object>() {
                            { "Brand", "Contoso" },
                            { "Model", "SSC4" },
                            { "Color", "White" }
                        })
                    ));
            individualEnrollment.Capabilities = OptionalEdgeCapabilityEnabled;
            individualEnrollment.IotHubHostName = IotHubHostName;       // This is mandatory if the DPS Allocation Policy is "Static"

            Console.WriteLine("\nAdding new individualEnrollment...");
            Console.WriteLine(JsonConvert.SerializeObject(individualEnrollment, Formatting.Indented));
            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment.RegistrationId, individualEnrollment).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(individualEnrollmentResult, Formatting.Indented));
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("\nGetting the individualEnrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(getResult, Formatting.Indented));

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            var individualEnrollment = await GetIndividualEnrollmentInfoAsync().ConfigureAwait(false);
            individualEnrollment.InitialTwin.Properties.Desired.AdditionalProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = OptionalEdgeCapabilityDisabled;

            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment.RegistrationId, individualEnrollment, individualEnrollment.Etag).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(individualEnrollmentResult, Formatting.Indented));
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            Console.WriteLine("\nDeleting the individualEnrollment...");
            await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
        }
    }
}
