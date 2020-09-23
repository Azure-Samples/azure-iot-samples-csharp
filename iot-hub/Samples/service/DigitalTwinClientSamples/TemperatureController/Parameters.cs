// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Parameters for the application supplied via command line arguments.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            Required = true,
            HelpText = "The IoT Hub connection string. This is available under the \"Shared access policies\" in the Azure portal.")]
        public string HubConnectionString { get; set; }

        [Option(
            'd',
            "DigitalTwinId",
            Required = true,
            HelpText = "The Id of the plug and play compatible device to be used in the sample.")]
        public string DigitalTwinId { get; set; }
    }
}