// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ThermostatSample
    {
        private static readonly Random Random = new Random();
        private readonly DigitalTwinClient _digitalTwinClient;
        private readonly string _digitalTwinId;
        private readonly ILogger _logger;

        public ThermostatSample(DigitalTwinClient client, string digitalTwinId, ILogger logger)
        {
            _digitalTwinClient = client ?? throw new ArgumentNullException(nameof(client));
            _digitalTwinId = digitalTwinId ?? throw new ArgumentNullException(nameof(digitalTwinId));
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ThermostatSample>();
        }

        public async Task RunSampleAsync()
        {
            // Get and print the digital twin
            await GetAndPrintDigitalTwin<ThermostatTwin>();

            // Update the targetTemperature property of the digital twin
            await UpdateDigitalTwin();

            // Invoke the getMaxMinReport command on the root level of the digital twin
            await InvokeGetMaxMinReportCommand();
        }

        private async Task<T> GetAndPrintDigitalTwin<T>()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<T>(_digitalTwinId);
            var thermostatTwin = getDigitalTwinResponse.Body;
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(thermostatTwin, Formatting.Indented)}");

            return thermostatTwin;
        }

        private async Task UpdateDigitalTwin()
        {
            // Choose a random value to assign to the targetTemperature property
            int desiredTargetTemperature = Random.Next(0, 100);

            var targetTemperaturePropertyName = "targetTemperature";
            var updateOperation = new UpdateOperationsUtility();

            // First let's take a look at when the property was updated and what was it set to.
            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<ThermostatTwin>(_digitalTwinId);
            WritableProperty currentTargetTemperature = getDigitalTwinResponse.Body.Metadata.TargetTemperature;
            if (currentTargetTemperature != null)
            {
                var targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.Body.Metadata.TargetTemperature.LastUpdateTime;
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was last updated on {targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                    $" with a value of {getDigitalTwinResponse.Body.Metadata.TargetTemperature.DesiredValue}.");

                updateOperation.AppendReplaceOp($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }
            else
            {
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was never set on the ${_digitalTwinId} digital twin.");

                updateOperation.AppendAddOp($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property on the {_digitalTwinId} digital twin to {desiredTargetTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());

            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {updateDigitalTwinResponse.Response.StatusCode}.");
        }

        private async Task InvokeGetMaxMinReportCommand()
        {
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            var getMaxMinReportCommandName = "getMaxMinReport";

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on {_digitalTwinId} digital twin.");

            HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient.InvokeCommandAsync(
                _digitalTwinId, 
                getMaxMinReportCommandName, 
                JsonConvert.SerializeObject(since));

            _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked: {invokeCommandResponse.Body.Payload}");
        }
    }
}
