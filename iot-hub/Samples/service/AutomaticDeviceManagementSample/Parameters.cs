// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using System;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Parameters for the application supplied via command line arguments.
    /// If the parameter is not supplied via command line args, it will look for it in environment variables.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            HelpText = "The IoT Hub connection string. This is available under the \"Shared access policies\" in the Azure portal." +
            "\nDefaults to environment variable \"IOTHUB_CONNECTION_STRING\".")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(HubConnectionString))
            {
                throw new ArgumentNullException(nameof(HubConnectionString), "An IoT Hub connection string needs to be specified, " +
                    "please set the environment variable \"IOTHUB_CONNECTION_STRING\" " +
                    "or pass in \"-c | --HubConnectionString\" through command line.");
            }

            return true;
        }
    }
}