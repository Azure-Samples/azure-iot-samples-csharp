﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            Required = true,
            HelpText = "The connection string of the IoT Hub instance to connect to.")]
        public string HubConnectionString { get; set; }

        [Option(
            'd',
            "DeviceId",
            Required = true,
            HelpText = "The Id of the device to connect to.")]
        public string DeviceId { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Amqp,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT Hub. Possible values include Amqp and Amqp_WebSocket_Only. The default value is Amqp.")]
        public TransportType TransportType { get; set; }
    }
}
