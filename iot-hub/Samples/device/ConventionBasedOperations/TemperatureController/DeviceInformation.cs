// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class DeviceInformation
    {
        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("swVersion")]
        public string SwVersion { get; set; }

        [JsonPropertyName("osName")]
        public string OsName { get; set; }

        [JsonPropertyName("processorArchitecture")]
        public string ProcessorArchitecture { get; set; }

        [JsonPropertyName("processorManufacturer")]
        public string ProcessorManufacturer { get; set; }

        [JsonPropertyName("totalStorage")]
        public double TotalStorage { get; set; }

        [JsonPropertyName("totalMemory")]
        public double TotalMemory { get; set; }
    }
}
