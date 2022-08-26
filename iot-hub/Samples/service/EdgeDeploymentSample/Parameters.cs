using CommandLine;
using System;

namespace Microsoft.Azure.Devices.Samples
{
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            HelpText = "The IoT hub connection string. This is available under the \"Shared access policies\" in the Azure portal." +
            "\nDefaults to environment variable \"IOTHUB_CONNECTION_STRING\".")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(HubConnectionString))
            {
                throw new ArgumentNullException(nameof(HubConnectionString), "An IoT hub connection string needs to be specified, " +
                    "please set the environment variable \"IOTHUB_CONNECTION_STRING\" " +
                    "or pass in \"-c | --HubConnectionString\" through command line.");
            }

            return true;
        }
    }
}