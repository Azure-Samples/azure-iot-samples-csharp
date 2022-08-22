// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;

namespace SimulatedDeviceWithCommand.Properties
{
    /// <summary>
    /// Command line parameters for the SimulatedDevice sample
    /// </summary>
    internal class Parameters
    {
        [Option(
           'p',
           "DeviceConnectionString",
           HelpText = "The IoT hub device connection string. This is available under the \"Devices\" in the Azure portal." +
           "\nDefaults to value of environment variable IOTHUB_DEVICE_CONNECTION_STRING.")]
        public string DeviceConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
    }
}
