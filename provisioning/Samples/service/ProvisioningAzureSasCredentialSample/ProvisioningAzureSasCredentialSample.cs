using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace ProvisioningAzureSasCredentialSample
{
    internal class ProvisioningAzureSasCredentialSample
    {
        private const string RegistrationIdPrefix = "sampleid-";
        private const string TpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";
        private string RegistrationId;

        ProvisioningServiceClient _provisioningServiceClient;
        public ProvisioningAzureSasCredentialSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
        }

        public async Task RunSampleAsync()
        {
            await QueryIndividualEnrollmentsAsync();

            await CreateIndividualEnrollmentAsync();
            await UpdateIndividualEnrollmentAsync();
            await DeleteIndividualEnrollmentAsync();
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a query for enrollments...");
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using (Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
            {
                while (query.HasNext())
                {
                    Console.WriteLine("\nQuerying the next enrollments...");
                    QueryResult queryResult = await query.NextAsync();
                    Console.WriteLine(queryResult);
                }
            }
        }

        public async Task CreateIndividualEnrollmentAsync()
        {
            Console.WriteLine("\nCreating a new individualEnrollment...");
            RegistrationId = RegistrationIdPrefix + Guid.NewGuid().ToString();
            Attestation attestation = new TpmAttestation(TpmEndorsementKey);
            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            RegistrationId,
                            attestation);

            individualEnrollment.InitialTwinState = new TwinState(
                null,
                new TwinCollection()
                {
                    ["Brand"] = "Contoso",
                    ["Model"] = "SSC4",
                    ["Color"] = "White",
                });

            Console.WriteLine("\nAdding new individualEnrollment...");
            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine(individualEnrollmentResult);
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("\nGetting the individualEnrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId);
            Console.WriteLine(getResult);

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            var individualEnrollment = await GetIndividualEnrollmentInfoAsync();
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";

            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine(individualEnrollmentResult);
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            Console.WriteLine("\nDeleting the individualEnrollment...");
            await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId);
        }
    }
}
