using CommandLine;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace X509DeviceCertWithChainSample
{
    internal class Parameters
    {
        [Option(
            'h',
            "hostName",
            Required = true,
            HelpText = "The hostname of IotHub.")]
        public string HostName { get; set; }

        [Option(
            'd',
            "deviceName",
            Required = true,
            HelpText = "The name of the device.")]
        public string DeviceName { get; set; }

        [Option(
            'p',
            "devicePfxPassword",
            Required = true,
            HelpText = "The password of device certificate.")]
        public string DevicePfxPassword { get; set; }

        [Option(
            'r',
            "rootCertPath",
            Required = true,
            HelpText = "Path to rootCA certificate.")]
        public string RootCertPath { get; set; }

        [Option(
            'i',
            "intermediate1CertPassword",
            Required = true,
            HelpText = "Path to intermediate 1 certificate.")]
        public string Intermediate1CertPassword { get; set; }

        [Option(
            'j',
            "intermediate2CertPassword",
            Required = true,
            HelpText = "Path to intermediate 2 certificate.")]
        public string Intermediate2CertPassword { get; set; }

        [Option(
            'k',
            "devicePfxPath",
            Required = true,
            HelpText = "Path to device pfx path.")]
        public string DevicePfxPath { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt_Tcp_Only,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT Hub. Possible values include Mqtt_Tcp_Only, Amqp_Tcp_only.")]
        public TransportType TransportType { get; set; }
    }
}
