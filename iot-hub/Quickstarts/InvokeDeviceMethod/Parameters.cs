using CommandLine;

namespace InvokeDeviceMethod
{
    /// <summary>
    /// Command line parameters for the InvokeDeviceMethod sample
    /// </summary>
    internal class Parameters
    {
        [Option(
            's',
            "ServiceConnectionString",
            Required = false,
            HelpText = "The connection string for the IoT hub.")]
        public string ServiceConnectionString { get; set; }

        [Option(
            'n',
            "DeviceName",
            Required = false,
            HelpText = "Name of the device to receive the direct method.")]
        public string DeviceName { get; set; }
    }
}