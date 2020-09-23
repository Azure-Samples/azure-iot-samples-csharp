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
            await UpdateTargetTemperatureProperty();

            // Add, replace then remove currentTemperature property on the digital twin. Note that the currentTemperature property does not exist on the
            // model that the device is registered with.
            await UpdateCurrentTemperatureProperty();

            // Invoke the root-level command getMaxMinReport command on the digital twin
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

        private async Task UpdateTargetTemperatureProperty()
        {
            const string targetTemperaturePropertyName = "targetTemperature";
            var updateOperation = new UpdateOperationsUtility();

            // Choose a random value to assign to the targetTemperature property
            int desiredTargetTemperature = Random.Next(0, 100);

            // First let's take a look at when the property was updated and what was it set to.
            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<ThermostatTwin>(_digitalTwinId);
            WritableProperty currentTargetTemperature = getDigitalTwinResponse.Body.Metadata.TargetTemperature;
            if (currentTargetTemperature != null)
            {
                var targetTemperatureDesiredLastUpdateTime = getDigitalTwinResponse.Body.Metadata.TargetTemperature.LastUpdateTime;
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was last updated on {targetTemperatureDesiredLastUpdateTime.ToLocalTime()} `" +
                    $" with a value of {getDigitalTwinResponse.Body.Metadata.TargetTemperature.DesiredValue}.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendReplaceOp($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }
            else
            {
                _logger.LogDebug($"The property {targetTemperaturePropertyName} was never set on the ${_digitalTwinId} digital twin.");

                // The property path to be added should be prepended with a '/'
                updateOperation.AppendAddOp($"/{targetTemperaturePropertyName}", desiredTargetTemperature);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property on the {_digitalTwinId} digital twin to {desiredTargetTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());

            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {updateDigitalTwinResponse.Response.StatusCode}.");
        }

        private async Task UpdateCurrentTemperatureProperty()
        {
            // Choose a random value to assign to the currentTemperature property
            int currentTemperature = Random.Next(0, 100);

            const string currentTemperaturePropertyName = "currentTemperature";
            var updateOperation = new UpdateOperationsUtility();

            // First, add the property to the digital twin
            updateOperation.AppendAddOp($"/{currentTemperaturePropertyName}", currentTemperature);
            _logger.LogDebug($"Add the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin with a value of {currentTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> addPropertyToDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());
            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {addPropertyToDigitalTwinResponse.Response.StatusCode}.");
            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwin<ThermostatTwin>();

            // Second, update the property to a different value
            int newCurrentTemperature = Random.Next(0, 100);
            updateOperation.AppendAddOp($"/{currentTemperaturePropertyName}", newCurrentTemperature);
            _logger.LogDebug($"Replace the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin with a value of {newCurrentTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> replacePropertyInDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());
            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {replacePropertyInDigitalTwinResponse.Response.StatusCode}.");
            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwin<ThermostatTwin>();

            // Third, remote the currentTemperature property
            updateOperation.AppendRemoveOp($"/{currentTemperaturePropertyName}");
            _logger.LogDebug($"Remove the {currentTemperaturePropertyName} property on the {_digitalTwinId} digital twin.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> removePropertyInDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());
            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {removePropertyInDigitalTwinResponse.Response.StatusCode}.");
            // Print the Thermostat digital twin
            await GetAndPrintDigitalTwin<ThermostatTwin>();
        }

        private async Task InvokeGetMaxMinReportCommand()
        {
            DateTimeOffset since = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(2));
            const string getMaxMinReportCommandName = "getMaxMinReport";

            _logger.LogDebug($"Invoke the {getMaxMinReportCommandName} command on {_digitalTwinId} digital twin.");

            HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient.InvokeCommandAsync(
                _digitalTwinId, 
                getMaxMinReportCommandName, 
                JsonConvert.SerializeObject(since));

            _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked. Report: {invokeCommandResponse.Body.Payload}");
        }
    }
}
