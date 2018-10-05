// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Service.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class EnrollmentGroupSample
    {
        private const string EnrollmentGroupId = "enrollmentgrouptest";
        ProvisioningServiceClient _provisioningServiceClient;
        X509Certificate2 _groupIssuerCertificate;
		private readonly string X509AttestationMechanism = "x509";
		private const string IotHubHostName = "my-iothub-hostname";

        public EnrollmentGroupSample(ProvisioningServiceClient provisioningServiceClient, X509Certificate2 groupIssuerCertificate)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _groupIssuerCertificate = groupIssuerCertificate;
        }

        public async Task RunSampleAsync()
        {
            await QueryEnrollmentGroupAsync().ConfigureAwait(false);

            await CreateEnrollmentGroupAsync().ConfigureAwait(false);
            await UpdateEnrollmentGroupAsync().ConfigureAwait(false);
            await DeleteEnrollmentGroupAsync().ConfigureAwait(false);
        }

        public async Task QueryEnrollmentGroupAsync()
        {
            Console.WriteLine("\nCreating a query for enrollmentGroups...");
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
            IList<EnrollmentGroup> queryResult = await _provisioningServiceClient.QueryEnrollmentGroupsAsync(querySpecification).ConfigureAwait(false);
            foreach (EnrollmentGroup enrollmentGroup in queryResult)
            {
                Console.WriteLine(JsonConvert.SerializeObject(enrollmentGroup, Formatting.Indented));
                await EnumerateRegistrationsInGroup(enrollmentGroup).ConfigureAwait(false);
            }
        }

        private async Task EnumerateRegistrationsInGroup(EnrollmentGroup group)
        {
            Console.WriteLine($"\nCreating a query for registrations within group '{group.EnrollmentGroupId}'...");
            IList<DeviceRegistrationState> deviceRegistrationStates = await _provisioningServiceClient.QueryDeviceRegistrationStatesAsync(group.EnrollmentGroupId).ConfigureAwait(false);
            foreach (DeviceRegistrationState deviceRegistrationState in deviceRegistrationStates)
            {
                Console.WriteLine(JsonConvert.SerializeObject(deviceRegistrationState, Formatting.Indented));
            }
        }

        public async Task CreateEnrollmentGroupAsync()
        {
            Console.WriteLine("\nCreating a new enrollmentGroup...");
            X509Attestation attestation = new X509Attestation(
                signingCertificates: new X509Certificates(
                    new X509CertificateWithInfo(Convert.ToBase64String(_groupIssuerCertificate.Export(X509ContentType.Cert)))
                ));
            AttestationMechanism attestationMechanism = new AttestationMechanism(X509AttestationMechanism, x509: attestation);
            EnrollmentGroup enrollmentGroup =
                    new EnrollmentGroup(
                            EnrollmentGroupId,
                            attestationMechanism);
            enrollmentGroup.IotHubHostName = IotHubHostName;        // This is mandatory if the DPS Allocation Policy is "Static"
            Console.WriteLine(JsonConvert.SerializeObject(enrollmentGroup, Formatting.Indented));

            Console.WriteLine("\nAdding new enrollmentGroup...");
            EnrollmentGroup enrollmentGroupResult =
                await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroupId, enrollmentGroup).ConfigureAwait(false);
            Console.WriteLine("\nEnrollmentGroup created with success.");
            Console.WriteLine(JsonConvert.SerializeObject(enrollmentGroupResult, Formatting.Indented));
        }

        public async Task UpdateEnrollmentGroupAsync()
        {
            EnrollmentGroup enrollmentGroup = await GetEnrollmentGroupInfoAsync().ConfigureAwait(false);
            enrollmentGroup.InitialTwin = new InitialTwin(
                null,
                new InitialTwinProperties(
                    new TwinCollection(
                        new Dictionary<string, object>()
                        {
                            { "Brand", "Contoso" }
                        })));
            Console.WriteLine("\nUpdating the enrollmentGroup information...");
            EnrollmentGroup getResult =
                await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroupId, enrollmentGroup, enrollmentGroup.Etag).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(getResult, Formatting.Indented));
        }


        public async Task<EnrollmentGroup> GetEnrollmentGroupInfoAsync()
        {
            Console.WriteLine("\nGetting the enrollmentGroup information...");
            EnrollmentGroup getResult =
                await _provisioningServiceClient.GetEnrollmentGroupAsync(EnrollmentGroupId).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(getResult, Formatting.Indented));
            return getResult;
        }

        public async Task DeleteEnrollmentGroupAsync()
        {
            Console.WriteLine("\nDeleting the enrollmentGroup...");
            await _provisioningServiceClient.DeleteEnrollmentGroupAsync(EnrollmentGroupId).ConfigureAwait(false);
        }
    }
}
