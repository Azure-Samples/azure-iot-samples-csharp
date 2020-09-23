// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class TemperatureControllerSample
    {
        private static readonly Random Random = new Random();
        private readonly DigitalTwinClient _digitalTwinClient;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public TemperatureControllerSample(DigitalTwinClient client, string digitalTwinId, ILogger logger)
        {
            _digitalTwinClient = client ?? throw new ArgumentNullException(nameof(client));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TemperatureControllerSample>();
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin
            await GetAndPrintDigitalTwin<TemperatureControllerTwin>();
        }

        private async Task<T> GetAndPrintDigitalTwin<T>()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<T>(_digitalTwinId);
            var thermostatTwin = getDigitalTwinResponse.Body;
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(thermostatTwin, Formatting.Indented)}");

            return thermostatTwin;
        }
    }
}
