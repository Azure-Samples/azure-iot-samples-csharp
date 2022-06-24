﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
    {
        [Option(
            's',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance")]
        public string IdScope { get; set; }

        [Option(
            'i',
            "Id",
            Required = true,
            HelpText = "The registration Id of the individual enrollment.")]
        public string Id { get; set; }

        [Option(
            'p',
            "PrimaryKey",
            Required = true,
            HelpText = "The primary key of the individual enrollment.")]
        public string PrimaryKey { get; set; }

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to. This defaults to global.azure-devices-provisioning.net.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            HelpText = "The transport to use to communicate with the device provisioning instance. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1. This defaults to Mqtt.")]
        public TransportType TransportType { get; set; }
    }
}