// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices;
using System;
using System.Threading.Tasks;

namespace ImportExportDevicesWithManagedIdentitySample
{
    /// <summary>
    /// A sample to illustrate how to perform import and export jobs using managed identity 
    /// to access the storage account. This sample will copy all the devices in the source hub
    /// to the destination hub.
    /// For this sample to succeed, the managed identity should be configured to access the 
    /// storage account used for import and export.
    /// For more information on configuration, see TODO <see href=""/>.
    /// For more information on managed identities, see <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview"/>
    /// </summary>
    public class ImportExportDevicesWithManagedidentitySample
    {
        public async Task RunSampleAsync(string sourceHubConnectionString,
            string destinationHubConnectionString,
            string blobContainerUri,
            ManagedIdentityType identityType,
            string userDefinedManagedIdentityResourceId = null)
        {
            if (identityType == ManagedIdentityType.UserDefined
                && string.IsNullOrWhiteSpace(userDefinedManagedIdentityResourceId))
            {
                    throw new ArgumentNullException(nameof(userDefinedManagedIdentityResourceId),
                        "userDefinedManagedIdentityResourceId is required if identityType is UserDefined.");
            }

            Console.WriteLine($"Exporting devices from source hub to {blobContainerUri}/devices.txt.");
            await ExportDevicesAsync(sourceHubConnectionString,
                blobContainerUri,
                identityType,
                userDefinedManagedIdentityResourceId);
            Console.WriteLine("Exporting devices completed.");

            Console.WriteLine($"Importing devices from {blobContainerUri}/devices.txt to destination hub.");
            await ImportDevicesAsync(destinationHubConnectionString,
                blobContainerUri,
                identityType,
                userDefinedManagedIdentityResourceId);  
            Console.WriteLine("Importing devices completed.");
        }

        public async Task ExportDevicesAsync(string hubConnectionString,
            string blobContainerUri,
            ManagedIdentityType identityType,
            string userDefinedManagedIdentityResourceId = null)
        {
            using RegistryManager srcRegistryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);            

            JobProperties jobProperties = new JobProperties
            {
                OutputBlobContainerUri = blobContainerUri,
                StorageAuthenticationType = StorageAuthenticationType.IdentityBased
            };

            // Configure the ManagedIdentity if identityType is UserDefined.
            // This value will be ignored if identityType is SystemDefined.
            // The default is SystemDefined if StorageAuthenticationType is set to IdentityBased.
            if (identityType == ManagedIdentityType.UserDefined)
            {
                jobProperties.Identity = new ManagedIdentity
                {
                    userAssignedIdentity = userDefinedManagedIdentityResourceId
                };
            }

            JobProperties jobResult = await srcRegistryManager
                .ExportDevicesAsync(jobProperties);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                jobResult = await srcRegistryManager.GetJobAsync(jobResult.JobId);
                if (jobResult.Status == JobStatus.Completed)
                {
                    break;
                }
                else if (jobResult.Status == JobStatus.Failed)
                {
                    throw new Exception("Export job failed.");
                }
                else if (jobResult.Status == JobStatus.Cancelled)
                {
                    throw new Exception("Export job was canceled.");
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        public async Task ImportDevicesAsync(string hubConnectionString,
            string blobContainerUri,
            ManagedIdentityType identityType,
            string userDefinedManagedIdentityResourceId = null)
        {
            using RegistryManager destRegistryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);

            JobProperties jobProperties = new JobProperties
            {
                InputBlobContainerUri = blobContainerUri,
                OutputBlobContainerUri = blobContainerUri,
                StorageAuthenticationType = StorageAuthenticationType.IdentityBased
            };

            // Configure the ManagedIdentity if identityType is UserDefined.
            // This value will be ignored if identityType is SystemDefined.
            // The default is SystemDefined if StorageAuthenticationType is set to IdentityBased.
            if (identityType == ManagedIdentityType.UserDefined)
            {
                jobProperties.Identity = new ManagedIdentity
                {
                    userAssignedIdentity = userDefinedManagedIdentityResourceId
                };
            }

            JobProperties jobResult = await destRegistryManager
                .ImportDevicesAsync(jobProperties);

            // Poll every 5 seconds to see if the job has finished executing.
            while (true)
            {
                jobResult = await destRegistryManager.GetJobAsync(jobResult.JobId);
                if (jobResult.Status == JobStatus.Completed)
                {
                    break;
                }
                else if (jobResult.Status == JobStatus.Failed)
                {
                    throw new Exception("Import job failed.");
                }
                else if (jobResult.Status == JobStatus.Cancelled)
                {
                    throw new Exception("Import job was canceled.");
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
