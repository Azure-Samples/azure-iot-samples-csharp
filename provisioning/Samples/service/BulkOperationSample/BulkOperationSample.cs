// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Service.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class BulkOperationSample
    {
        private ProvisioningServiceClient _provisioningServiceClient;
        private const string SampleRegistrationId1 = "myvalid-registratioid-csharp-1";
        private const string SampleRegistrationId2 = "myvalid-registratioid-csharp-2";
        private const string SampleTpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

		private const string IotHubHostName = "my-iothub-hostname";
        // Maximum number of elements per query.
        private const int QueryPageSize = 2;

        private static IDictionary<string, string> _registrationIds = new Dictionary<string, string>
        {
            { SampleRegistrationId1, SampleTpmEndorsementKey },
            { SampleRegistrationId2, SampleTpmEndorsementKey }
        };

        private readonly string TpmAttestationType = "tpm";
        private readonly string BulkOperationCreate = "create";
        private readonly string BulkOperationUpdate = "update";
        private readonly string BulkOperationDelete = "delete";
        
        public BulkOperationSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
        }

        public async Task RunSampleAsync()
        {
            List<IndividualEnrollment> enrollments = await CreateBulkIndividualEnrollmentsAsync().ConfigureAwait(false);
            await UpdateBulkIndividualEnrollmentAsync(enrollments).ConfigureAwait(false);
            await DeleteBulkIndividualEnrollmentsAsync(enrollments).ConfigureAwait(false);
        }

        public async Task<List<IndividualEnrollment>> CreateBulkIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a new set of individualEnrollments...");
            List<IndividualEnrollment> individualEnrollments = new List<IndividualEnrollment>();
            foreach (var item in _registrationIds)
            {
                TpmAttestation attestation = new TpmAttestation(item.Value);
                AttestationMechanism attestationMechanism = new AttestationMechanism(TpmAttestationType, attestation);

                IndividualEnrollment individualEnrollment = new IndividualEnrollment(item.Key, attestationMechanism, iotHubHostName: IotHubHostName);   // "iotHubHostName" is mandatory if the DPS Allocation Policy is "Static"
                individualEnrollments.Add(individualEnrollment);
            }
            BulkEnrollmentOperation bulkEnrollmentOperation = new BulkEnrollmentOperation(individualEnrollments, BulkOperationCreate);
            Console.WriteLine("\nRunning the bulk operation to create the individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult =
                await _provisioningServiceClient.RunBulkEnrollmentOperationAsync(bulkEnrollmentOperation).ConfigureAwait(false);
            Console.WriteLine("\nResult of the Create bulk enrollment.");
            Console.WriteLine(JsonConvert.SerializeObject(bulkEnrollmentOperationResult, Formatting.Indented));

            return individualEnrollments;
        }

        public async Task UpdateBulkIndividualEnrollmentAsync(List<IndividualEnrollment> individualEnrollments)
        {
            List<IndividualEnrollment> updatedEnrollments = new List<IndividualEnrollment>();
            foreach (IndividualEnrollment individualEnrollment in individualEnrollments)
            {
                String registrationId = individualEnrollment.RegistrationId;
                Console.WriteLine($"\nGetting the {nameof(individualEnrollment)} information for {registrationId}...");
                IndividualEnrollment enrollment =
                    await _provisioningServiceClient.GetIndividualEnrollmentAsync(registrationId).ConfigureAwait(false);
                enrollment.DeviceId = "updated_the_device_id";
                updatedEnrollments.Add(enrollment);
            }
            BulkEnrollmentOperation bulkEnrollmentOperation = new BulkEnrollmentOperation(updatedEnrollments, BulkOperationUpdate);
            Console.WriteLine("\nRunning the bulk operation to update the individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult =
                await _provisioningServiceClient.RunBulkEnrollmentOperationAsync(bulkEnrollmentOperation).ConfigureAwait(false);
            Console.WriteLine("\nResult of the Update bulk enrollment.");
            Console.WriteLine(JsonConvert.SerializeObject(bulkEnrollmentOperationResult, Formatting.Indented));
        }

        public async Task DeleteBulkIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            BulkEnrollmentOperation bulkEnrollmentOperation = new BulkEnrollmentOperation(individualEnrollments, BulkOperationDelete);
            Console.WriteLine("\nDeleting the set of IndividualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult =
                await _provisioningServiceClient.RunBulkEnrollmentOperationAsync(bulkEnrollmentOperation).ConfigureAwait(false);
            Console.WriteLine(bulkEnrollmentOperationResult.IsSuccessful);
        }
    }
}
