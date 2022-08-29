using System;
using CommandLine;

namespace SimulatedDevice
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "PrimaryConnectionString",
            Required = true,
            HelpText = "The primary connection string for the device to simulate.")]
        public string PrimaryConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");

        [Option(
            'd',
            "DeviceId",
            Required = true,
            HelpText = "The device ID that you assigned when registering the device.")]
        public string DeviceId { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_DPS_DEVICE_ID");

        [Option(
            'k',
            "DeviceKey",
            Required = true,
            HelpText = "Find your IoT hub in the portal > IoT devices > select your device > copy the key.")]
        public string DeviceKey { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_DPS_DEVICE_KEY");

        [Option(
            "ReadTheFile",
            Required = false,
            HelpText = "If this is false, it will submit messages to the iot hub. If this is true, it will read one of the output files and convert it to ASCII.")]
        public bool ReadTheFile { get; set; } = false;
    }
}
