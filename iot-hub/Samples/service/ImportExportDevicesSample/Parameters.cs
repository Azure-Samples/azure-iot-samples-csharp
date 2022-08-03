using System;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    internal class Parameters
    {
        /// <summary>
        /// The source IoT Hub connection string.
        /// </summary>
        /// <remarks>
        /// You can get this from the portal:
        /// 1. Log into https://azure.portal.com, go to Resources, find your IoT hub and select it.
        /// 2. Then look for Shared Access Policies and select it.
        /// 3. Then select 'iothubowner' and copy one of the connection strings.
        /// </remarks>
        [Option(
            'i',
            "SourceIoTHubConnectionString",
            Required = true,
            HelpText = "The service connection string with permissions to manage devices for the source IoT hub to copy devices.")]
        public string SourceIotHubConnectionString { get; set; } = Environment.GetEnvironmentVariable("SOURCE_IOTHUB_CONN_STRING_CSHARP");

        /// <summary>
        /// When copying data from one hub to another, this is the connection string
        /// to the destination hub, i.e. the new one.
        /// </summary>
        [Option(
            'd',
            "DestIoTHubConnectionString",
            Required = true,
            HelpText = "The service connection string with permissions to manage devices for the destination IoT hub to migrate devices.")]
        public string DestIotHubConnectionString { get; set; } = Environment.GetEnvironmentVariable("DEST_IOTHUB_CONN_STRING_CSHARP");

        /// <summary>
        /// Connection string to the storage account used to hold the imported or exported data.
        /// </summary>
        /// <remarks>
        /// Log into https://azure.portal.com, go to Resources, find your storage account and select it.
        /// Select Access Keys and copy one of the connection strings.
        /// </remarks>
        [Option(
            's',
            "StorageConnectionString",
            Required = true,
            HelpText = "The connection string for the storage account to use for device migration data.")]
        public string StorageConnectionString { get; set; } = Environment.GetEnvironmentVariable("STORAGE_CONN_STRING_CSHARP");

        [Option(
            "AddDevices",
            Default = 0,
            HelpText = "Generates the specified number of new devices and add to the source hub, for migration to the destination hub.")]
        public int AddDevices { get; set; }

        [Option(
            "CopyDevices",
            Default = true,
            HelpText = "Copies devices from the source to the destionation IoT hub.")]
        public bool CopyDevices { get; set; }

        [Option(
            "DeleteSourceDevices",
            Default = false,
            HelpText = "Deletes generated devices in the source IoT hub, after migration and this sample is finished.")]
        public bool DeleteSourceDevices { get; set; }

        [Option(
            "DeleteDestDevices",
            Default = false,
            HelpText = "Delete the devices that were migrated in the destionation IoT hub, after migration and this sample is finished.")]
        public bool DeleteDestDevices { get; set; }

        /// <summary>
        /// Loads up from environment variables for types that require parsing.
        /// </summary>
        public Parameters()
        {
            string addDevicesEnv = Environment.GetEnvironmentVariable("NUM_TO_ADD");
            if (!string.IsNullOrWhiteSpace(addDevicesEnv)
                && int.TryParse(addDevicesEnv, out int addDevices))
            {
                AddDevices = addDevices;
            }

            string copyDevicesEnv = Environment.GetEnvironmentVariable("COPY_DEVICES");
            if (!string.IsNullOrWhiteSpace(copyDevicesEnv)
                && bool.TryParse(copyDevicesEnv, out bool copyDevices))
            {
                CopyDevices = copyDevices;
            }

            string deleteFromDevicesEnv = Environment.GetEnvironmentVariable("DELETE_SOURCE_DEVICES");
            if (!string.IsNullOrWhiteSpace(deleteFromDevicesEnv)
                && bool.TryParse(deleteFromDevicesEnv, out bool deleteFromDevices))
            {
                DeleteSourceDevices = deleteFromDevices;
            }

            string deleteToDevicesEnv = Environment.GetEnvironmentVariable("DELETE_DEST_DEVICES");
            if (!string.IsNullOrWhiteSpace(deleteToDevicesEnv)
                && bool.TryParse(deleteToDevicesEnv, out bool deleteToDevices))
            {
                DeleteSourceDevices = deleteToDevices;
            }
        }
    }
}
