// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    public class ImportExportDevicesSample
    {
        private readonly string _srcIotHubConnectionString;
        private readonly string _destIotHubConnectionString;
        private readonly string _storageAccountConnectionString;
        private readonly string _containerName;
        private readonly string _blobName;

        // The container used to hold the blob containing the list of import/export files.
        // This is a sample-wide variable. If this project doesn't find this container, it will create it.
        private CloudBlobContainer _cloudBlobContainer;

        private string _containerUriWithSas;

        public ImportExportDevicesSample(
            string sourceIotHubConnectionString,
            string destinationHubConnectionString,
            string sourceStorageAccountConnectionString,
            string containerName,
            string blobName)
        {
            _destIotHubConnectionString = destinationHubConnectionString;
            _srcIotHubConnectionString = sourceIotHubConnectionString;
            _storageAccountConnectionString = sourceStorageAccountConnectionString;
            _containerName = containerName;
            _blobName = blobName;
        }

        public async Task RunSampleAsync(
            int devicesToAdd,
            bool shouldCopyDevices,
            bool shouldDeleteSourceDevices,
            bool shouldDeleteDestDevices)
        {
            // This sets cloud blob container and returns container uri (w/shared access token).
            _containerUriWithSas = await PrepareStorageForImportExportAsync(_storageAccountConnectionString).ConfigureAwait(false);

            if (devicesToAdd > 0)
            {
                // generate and add new devices
                await GenerateDevicesAsync(_srcIotHubConnectionString, devicesToAdd).ConfigureAwait(false);
            }

            if (shouldCopyDevices)
            {
                // Copy devices from the original hub to a new hub
                await CopyToDestHubAsync(_srcIotHubConnectionString, _destIotHubConnectionString).ConfigureAwait(false);
            }

            if (shouldDeleteSourceDevices)
            {
                // delete devices from the source hub
                await DeleteFromHubAsync(_srcIotHubConnectionString).ConfigureAwait(false);
            }

            if (shouldDeleteDestDevices)
            {
                // delete devices from the destination hub
                await DeleteFromHubAsync(_destIotHubConnectionString).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets up references to the blob hierarchy objects, sets containerURI with an SAS for access.
        /// Create the container if it doesn't exist.
        /// </summary>
        /// <returns>URI to blob container, including SAS token</returns>
        private async Task<string> PrepareStorageForImportExportAsync(string storageAccountConnectionString)
        {
            Console.WriteLine("Preparing storage.");

            string containerUri;
            try
            {
                // Get reference to storage account.
                // This is the storage account used to hold the import and export file lists.
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

                // Get reference to the blob client.
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                // Get reference to the container to be used.
                _cloudBlobContainer = cloudBlobClient.GetContainerReference(_containerName);

                // Get the URI to the container. This doesn't have an SAS token (yet).
                containerUri = _cloudBlobContainer.Uri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up storage account. Msg = {ex.Message}");
                throw;
            }

            // How to get reference to a blob. 
            //   Just leaving this in here in case you want to copy this block of code and use it for this.
            // CloudBlockBlob cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(deviceListFile);
            // This is how you get the URI to the blob. The import and export both use a container
            //   with container-level access, so this isn't used for importing or exporting the devices.
            // Just leaving it in here for when it's needed.
            // string blobURI = blockBlob.Uri.ToString();

            try
            {
                // The call below will fail if the sample is configured to use the storage emulator 
                //   in the connection string but the emulator is not running.
                // Change the retry policy for this call so that if it fails, it fails quickly.
                var requestOptions = new BlobRequestOptions { RetryPolicy = new NoRetry() };
                await _cloudBlobContainer.CreateIfNotExistsAsync(requestOptions, null).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not StorageException)
            {
            }

            // Call to get the SAS token for container-level access.
            string containerSASToken = GetContainerSasToken(_cloudBlobContainer);

            // Append the SAS token to the URI to the container. This is returned.
            _containerUriWithSas = containerUri + containerSASToken;
            return _containerUriWithSas;
        }

        /// <summary>
        /// Create the SAS token for the container.
        /// </summary>
        private static string GetContainerSasToken(CloudBlobContainer container)
        {
            // Set the expiry time and permissions for the container.
            // In this case no start time is specified, so the
            // shared access signature becomes valid immediately.
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Write
                    | SharedAccessBlobPermissions.Read
                    | SharedAccessBlobPermissions.Delete,
            };

            // Generate the shared access signature on the container,
            // setting the constraints directly on the signature.
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the SAS Token
            // Concatenate it on the URI to the resource to get what you need to access the resource.
            return sasContainerToken;
        }

        /// <summary>
        /// Add devices to the hub; specify how many. This creates a number of devices
        ///   with partially random hub names. 
        /// This is a good way to test the import -- create a bunch of devices on one hub,
        ///    then use this the Copy feature to copy the devices to another hub.
        /// Number of devices to create and add. Default is 10.
        /// </summary>
        public async Task GenerateDevicesAsync(string hubConnectionString, int numToAdd)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"Creating {numToAdd} devices for the source IoT hub.");

            await GenerateAndAddDevicesAsync(hubConnectionString, _containerUriWithSas, numToAdd).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"GenerateDevices, time elapsed = {stopwatch.Elapsed}.");
        }

        /// <summary>
        ///  This reads the device list from the IoT hub, then writes them to a file in blob storage.
        /// </summary>
        public async Task ExportToBlobStorageAsync(string hubConnectionString)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Exporting devices to blob storage.");

            await ExportDevicesAsync(_containerUriWithSas, hubConnectionString).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"Exported devices to blob storage: time elapsed = {stopwatch.Elapsed}");
        }

        /// <summary>
        /// Delete all of the devices from the hub with the given connection string.
        /// </summary>
        /// <param name="hubConnectionString">Connection to the hub from which you want to delete the devices.</param>
        public async Task DeleteFromHubAsync(string hubConnectionString)
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Deleting all devices from an IoT hub.");
            await DeleteAllDevicesFromHubAsync(hubConnectionString, _containerUriWithSas).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"Deleted IoT hub devices: time elapsed = {stopwatch.Elapsed}");
        }

        /// <summary>
        /// Copy the devices from one hub to another. 
        /// </summary>
        /// <param name="prevHubConnectionString">Connection string for source hub.</param>
        /// <param name="newHubConnectionString">Connection string for destination hub.</param>
        public async Task CopyToDestHubAsync(string prevHubConnectionString, string newHubConnectionString)
        {
            Console.WriteLine("Copying devices from the source to the destination IoT hub.");
            var stopwatch = Stopwatch.StartNew();

            await CopyAllDevicesToDestinationHubAsync(
                    prevHubConnectionString,
                    newHubConnectionString,
                    _containerUriWithSas)
                .ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"Copied devices: time elapsed = {stopwatch.Elapsed}");
        }

        /// <summary>
        /// Generate NumToAdd devices and add them to the hub.
        /// To do this, generate each identity.
        /// Include authentication keys.
        /// Write the device info to a block blob.
        /// Import the devices into the identity registry by calling the import job.
        /// </summary>
        private async Task GenerateAndAddDevicesAsync(string hubConnectionString, string containerUri, int numToAdd)
        {
            int interimProgressCount = 0;
            int displayProgressCount = 1000;
            int totalProgressCount = 0;

            // generate reference for list of new devices we're going to add, will write list to this blob
            CloudBlockBlob generatedListBlob = _cloudBlobContainer.GetBlockBlobReference(_blobName);

            // define serializedDevices as a generic list<string>
            var serializedDevices = new List<string>(numToAdd);

            for (int i = 1; i <= numToAdd; i++)
            {
                // Create device name with this format: Hub_00000000 + a new guid.
                // This should be large enough to display the largest number (1 million).
                string deviceName = $"Hub_{i:D8}_{Guid.NewGuid()}";
                Debug.Print($"Adding device '{deviceName}'");

                // Create a new ExportImportDevice.
                var deviceToAdd = new ExportImportDevice
                {
                    Id = deviceName,
                    Status = DeviceStatus.Enabled,
                    Authentication = new AuthenticationMechanism
                    {
                        SymmetricKey = new SymmetricKey
                        {
                            PrimaryKey = GenerateKey(32),
                            SecondaryKey = GenerateKey(32),
                        }
                    },
                    // This indicates that the entry should be added as a new device.
                    ImportMode = ImportMode.Create,
                };

                // Add device to the list as a serialized object.
                serializedDevices.Add(JsonConvert.SerializeObject(deviceToAdd));

                // Not real progress as you write the new devices, but will at least show *some* progress.
                interimProgressCount++;
                totalProgressCount++;
                if (interimProgressCount >= displayProgressCount)
                {
                    Console.WriteLine($"Added {totalProgressCount}/{numToAdd} devices.");
                    interimProgressCount = 0;
                }
            }

            // Now have a list of devices to be added, each one has been serialized.
            // Write the list to the blob.
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice => sb.AppendLine(serializedDevice));

            // Before writing the new file, make sure there's not already one there.
            await generatedListBlob.DeleteIfExistsAsync().ConfigureAwait(false);

            // Write list of serialized objects to the blob.
            using CloudBlobStream stream = await generatedListBlob.OpenWriteAsync().ConfigureAwait(false);
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            for (int i = 0; i < bytes.Length; i += 500)
            {
                int length = Math.Min(bytes.Length - i, 500);
                await stream.WriteAsync(bytes.AsMemory(i, length)).ConfigureAwait(false);
            }
            stream.Commit();

            Console.WriteLine("Running a registry manager job to add the devices.");

            // Should now have a file with all the new devices in it as serialized objects in blob storage.
            // generatedListBlob has the list of devices to be added as serialized objects.
            // Call import using the blob to add the new devices.
            // Log information related to the job is written to the same container.
            // This normally takes 1 minute per 100 devices (according to the docs).

            // First, initiate an import job.
            // This reads in the rows from the text file and writes them to IoT Devices.
            // If you want to add devices from a file, you can create a file and use this to import it.
            //   They have to be in the exact right format.
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            try
            {
                // The first URL is the container to import from; the file must be called devices.txt
                // The second URL points to the container to write errors to as a block blob.
                // This lets you import the devices from any file name. Since we wrote the new
                // devices to [devicesToAdd], need to read the list from there as well.
                JobProperties importJob = await registryManager
                    .ImportDevicesAsync(containerUri, containerUri)
                    .ConfigureAwait(false);
                await WaitForJobAsync(registryManager, importJob).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Adding devices failed due to {ex.Message}");
            }
        }

        /// Get the list of devices registered to the IoT Hub 
        ///   and export it to a blob as deserialized objects.
        private static async Task ExportDevicesAsync(string containerUri, string hubConnectionString)
        {
            try
            {
                Console.WriteLine("Running a registry manager job to export devices from the hub.");
                // Create an instance of the registry manager class.
                using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);

                // Call an export job on the IoT Hub to retrieve all devices.
                // This writes them to devices.txt in the container.
                JobProperties exportJob = await registryManager
                    .ExportDevicesAsync(containerUri, excludeKeys: false)
                    .ConfigureAwait(false);
                await WaitForJobAsync(registryManager, exportJob).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting devices to blob storage. Exception message = {ex.Message}");
            }
        }

        // This shows how to delete all of the devices for the IoT Hub.
        // First, export the list to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object;
        // read them into the generic list serializedDevices.
        // Delete the devices.txt in blob storage, because we're going to recreate it.
        // For each serializedDevice, deserialize it, set ImportMode to Delete, 
        // reserialize it, and write it to a StringBuilder. The ImportMode field is what
        // tells the job framework to delete each one.
        // Write the new StringBuilder to the block blob.
        // This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in devices.txt, then delete each one. 
        private async Task DeleteAllDevicesFromHubAsync(string hubConnectionString, string containerUri)
        {
            Console.WriteLine("Exporting a list of devices from IoT hub to blob storage.");

            // Read the devices from the hub and write them to devices.txt in blob storage.
            await ExportDevicesAsync(containerUri, hubConnectionString).ConfigureAwait(false);

            // Read devices.txt which contains serialized objects. 
            // Write each line to the serializedDevices list. (List<string>). 
            CloudBlockBlob blockBlob = _cloudBlobContainer.GetBlockBlobReference(_blobName);

            // Get the URI for the blob.
            string blobUri = blockBlob.Uri.ToString();

            // Instantiate the generic list.
            var serializedDevices = new List<string>();

            Console.WriteLine("Reading the list of devices in from blob storage.");

            // Read the blob file of devices, import each row into serializedDevices.
            using Stream blobStream = await blockBlob
                .OpenReadAsync(AccessCondition.GenerateIfExistsCondition(), null, null)
                .ConfigureAwait(false);
            using var streamReader = new StreamReader(blobStream, Encoding.UTF8);
            while (streamReader.Peek() != -1)
            {
                string line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                serializedDevices.Add(line);
            }

            // Delete the blob containing the list of devices, because we're going to recreate it.
            CloudBlockBlob blobToDelete = _cloudBlobContainer.GetBlockBlobReference(_blobName);

            Console.WriteLine("Updating ImportMode to be 'Delete' for each device and writing back to the blob.");

            // Step 1: Update each device's ImportMode to be Delete
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice and change import mode.
                var device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);
                device.ImportMode = ImportMode.Delete;

                // Reserialize the object now that we've updated the property.
                sb.AppendLine(JsonConvert.SerializeObject(device));
            });

            // Step 2: Delete the blob if it already exists, then write the list in memory to the blob.
            await blobToDelete.DeleteIfExistsAsync().ConfigureAwait(false);
            using CloudBlobStream stream = await blobToDelete.OpenWriteAsync().ConfigureAwait(false);
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            for (int i = 0; i < bytes.Length; i += 500)
            {
                int length = Math.Min(bytes.Length - i, 500);
                await stream.WriteAsync(bytes.AsMemory(i, length)).ConfigureAwait(false);
            }
            stream.Commit();

            Console.WriteLine("Running a registry manager job to delete the devices from the IoT hub.");

            // Step 3: Call import using the same blob to delete all devices.
            // Loads devices.txt and applies that change.
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            JobProperties importJob = await registryManager
                .ImportDevicesAsync(containerUri, containerUri)
                .ConfigureAwait(false);
            await WaitForJobAsync(registryManager, importJob).ConfigureAwait(false);
        }

        // This shows how to copy devices from one IoT hub to another.
        // First, export the list from the Source hut to devices.txt (ExportDevices).
        // Next, read in that file. Each row is a serialized object;
        //   read them into the generic list serializedDevices.
        // Delete the devices.txt in blob storage, because we're going to recreate it.
        // For each serializedDevice, deserialize it, set ImportMode to Create,
        //   reserialize it, and write it to a StringBuilder. The ImportMode field is what
        //   tells the job framework to add each device.
        // Write the new StringBuilder to the block blob.
        //   This essentially replaces the list with a list of devices that have ImportJob = Delete.
        // Call ImportDevicesAsync, which will read in the list in the blob, then add each one
        //   because it doesn't already exist. If it already exists, it will write an entry to
        //   the import error log and not add the new one.
        private async Task CopyAllDevicesToDestinationHubAsync(string sourceHubConnectionString, string destHubConnectionString, string containerUri)
        {
            Console.WriteLine("Exporting devices to destination IoT hub.");

            // Read the devices from the hub and write them to devices.txt in blob storage.
            await ExportDevicesAsync(containerUri, sourceHubConnectionString).ConfigureAwait(false);

            // Read devices.txt which contains serialized objects. 
            // Write each line to the serializedDevices list. (List<string>). 
            CloudBlockBlob blockBlob = _cloudBlobContainer.GetBlockBlobReference(_blobName);

            // Get the URI for the blob.
            string blobUri = blockBlob.Uri.ToString();

            // Instantiate the generic list.
            var serializedDevices = new List<string>();

            Console.WriteLine("Reading in the list of devices from blob storage.");

            // Read the blob file of devices, import each row into serializedDevices.
            using Stream blobStream = await blockBlob.OpenReadAsync(AccessCondition.GenerateIfExistsCondition(), null, null).ConfigureAwait(false);
            using var streamReader = new StreamReader(blobStream, Encoding.UTF8);
            while (streamReader.Peek() != -1)
            {
                string line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                serializedDevices.Add(line);
            }

            // Delete the blob containing the list of devices, because we're going to recreate it.
            CloudBlockBlob blobToDelete = _cloudBlobContainer.GetBlockBlobReference("devices.txt");

            Console.WriteLine("Updating ImportMode to be Create.");

            // Step 1: Update each device's ImportMode to Create
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice =>
            {
                // Deserialize back to an ExportImportDevice and update the import mode property.
                ExportImportDevice device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);
                device.ImportMode = ImportMode.Create;

                // Reserialize the object now that we've updated the property.
                sb.AppendLine(JsonConvert.SerializeObject(device));
            });

            // Step 2: Delete the blob if it already exists, then write the in-memory list to the blob.
            await blobToDelete.DeleteIfExistsAsync().ConfigureAwait(false);

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            using CloudBlobStream stream = await blobToDelete.OpenWriteAsync().ConfigureAwait(false);
            for (int i = 0; i < bytes.Length; i += 500)
            {
                int length = Math.Min(bytes.Length - i, 500);
                await stream.WriteAsync(bytes.AsMemory(i, length)).ConfigureAwait(false);
            }
            stream.Commit();

            Console.WriteLine("Running a registry manager job to import the entries from the devices file to the destination IoT hub.");

            // Step 3: Call import using the same blob to create all devices.
            // Loads and adds the devices to the destination IoT hub.
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(destHubConnectionString);
            JobProperties importJob = await registryManager.ImportDevicesAsync(containerUri, containerUri).ConfigureAwait(false);
            await WaitForJobAsync(registryManager, importJob).ConfigureAwait(false);
        }

        private static string GenerateKey(int keySize)
        {
            byte[] keyBytes = new byte[keySize];
            using var cyptoProvider = RandomNumberGenerator.Create();
            while (keyBytes.Contains(byte.MinValue))
            {
                cyptoProvider.GetBytes(keyBytes);
            }

            return Convert.ToBase64String(keyBytes);
        }

        private static async Task WaitForJobAsync(RegistryManager registryManager, JobProperties job)
        {
            // Wait until job is finished
            while (true)
            {
                job = await registryManager.GetJobAsync(job.JobId).ConfigureAwait(false);
                if (job.Status == JobStatus.Completed
                    || job.Status == JobStatus.Failed
                    || job.Status == JobStatus.Cancelled)
                {
                    // Job has finished executing
                    break;
                }
                Console.WriteLine($"\tJob status is {job.Status}...");

                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }

            Console.WriteLine($"Job finished with status of {job.Status}.");
        }
    }
}
