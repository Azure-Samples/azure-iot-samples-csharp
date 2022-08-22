// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;

namespace SimulatedDevice
{
    /// <summary>
    /// Command line parameters for the InvokeDeviceMethod sample
    /// </summary>
    internal class Parameters
    {
        [Option(
           'c',
           "HubConnectionString",
           HelpText = "The IoT hub connection string. This is available under the \"Shared access policies\" in the Azure portal." +
           "\nDefaults to value of environment variable IOTHUB_CONNECTION_STRING.")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
    }
}
