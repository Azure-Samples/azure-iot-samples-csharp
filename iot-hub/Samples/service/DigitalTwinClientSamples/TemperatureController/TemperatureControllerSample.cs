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

            // Update the targetTemperature property on the thermostat1 component
            await UpdateDigitalTwinComponentProperty();

            // Invoke the getMaxMinReport command on the thermostat1 component of the TemperatureController digital twin
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

        private async Task UpdateDigitalTwinComponentProperty()
        {
            // Choose a random value to assign to the targetTemperature property in thermostat1 component
            int desiredTargetTemperature = Random.Next(0, 100);

            const string targetTemperaturePropertyName = "targetTemperature";
            const string componentName = "thermostat1";
            var updateOperation = new UpdateOperationsUtility();

            // First let's take a look at when the property was updated and what was it set to.
            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<TemperatureControllerTwin>(_digitalTwinId);
            WritableProperty currentComponentTargetTemperature = getDigitalTwinResponse.Body.Thermostat1.Metadata.TargetTemperature;
            if (currentComponentTargetTemperature != null)
            {
                var targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.Body.Thermostat1.Metadata.TargetTemperature.LastUpdateTime;
                _logger.LogDebug($"The property {targetTemperaturePropertyName} under component {componentName} was last updated on {targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                    $" with a value of {getDigitalTwinResponse.Body.Thermostat1.Metadata.TargetTemperature.DesiredValue}.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendReplaceOp($"/{componentName}/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }
            else
            {
                _logger.LogDebug($"The property {targetTemperaturePropertyName} under component {componentName} was never set on the {_digitalTwinId} digital twin.");

                // The property path to be added should be prepended with a '/'
                updateOperation.AppendAddOp($"/{componentName}/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property under component {componentName} on the {_digitalTwinId} digital twin to {desiredTargetTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());

            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {updateDigitalTwinResponse.Response.StatusCode}.");
        }

        private async Task InvokeGetMaxMinReportCommand()
        {
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            const string getMaxMinReportCommandName = "getMaxMinReport";
            const string componentName = "thermostat1";

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on component {componentName} in the {_digitalTwinId} digital twin.");

            HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient.InvokeComponentCommandAsync(
                _digitalTwinId, 
                componentName, 
                getMaxMinReportCommandName, 
                JsonConvert.SerializeObject(since));

            _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on component {componentName}. Report: {invokeCommandResponse.Body.Payload}");
        }
    }
}
