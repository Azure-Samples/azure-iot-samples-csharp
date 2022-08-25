using System;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples.JobsSample
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'p',
            "HubConnectionString",
            Required = false,
            HelpText = "The connection string of the IoT hub instance to connect to. This can be located under \"Shared Access Policies\" in the Iot hub.")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
    }
}
