// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.PlugAndPlay;
using Microsoft.Azure.Devices.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            await GetAndPrintDigitalTwinAsync<TemperatureControllerTwin>();

            // Update the targetTemperature property on the thermostat1 component
            await UpdateDigitalTwinComponentPropertyAsync();

            // Invoke the component-level command getMaxMinReport on the thermostat1 component of the TemperatureController digital twin
            await InvokeGetMaxMinReportCommandAsync();

            // Invoke the root-level command reboot on the TemperatureController digital twin
            await InvokeRebootCommandAsync();
        }

        private async Task<T> GetAndPrintDigitalTwinAsync<T>()
        {
            _logger.LogDebug($"Get the {_digitalTwinId} digital twin.");

            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<T>(_digitalTwinId);
            var thermostatTwin = getDigitalTwinResponse.Body;
            _logger.LogDebug($"{_digitalTwinId} twin: \n{JsonConvert.SerializeObject(thermostatTwin, Formatting.Indented)}");

            return thermostatTwin;
        }

        private async Task UpdateDigitalTwinComponentPropertyAsync()
        {
            // Choose a random value to assign to the targetTemperature property in thermostat1 component
            int desiredTargetTemperature = Random.Next(0, 100);

            const string targetTemperaturePropertyName = "targetTemperature";
            const string componentName = "thermostat1";
            var updateOperation = new UpdateOperationsUtility();

            // First let's take a look at when the property was updated and what was it set to.
            var getDigitalTwinResponse = await _digitalTwinClient.GetDigitalTwinAsync<TemperatureControllerTwin>(_digitalTwinId);
            ThermostatTwin thermostat1 = getDigitalTwinResponse.Body.Thermostat1;
            if (thermostat1 != null)
            {
                // Thermostat1 is present in the TemperatureController twin. We can add/ replace the component-level property "targetTemperature".
                double? currentComponentTargetTemperature = getDigitalTwinResponse.Body.Thermostat1.TargetTemperature;
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
            }
            else
            {
                // Thermostat1 is not present in the TemperatureController twin. We will add the component.
                var componentProperty = new Dictionary<string, object> { { targetTemperaturePropertyName, desiredTargetTemperature } };
                var componentValuePatch = PnpHelper.CreatePatchValueForComponentUpdate(componentProperty);
                _logger.LogDebug($"The component {componentName} does not exist on the {_digitalTwinId} digital twin.");

                // The property path to be replaced should be prepended with a '/'
                updateOperation.AppendAddOp($"/{componentName}", componentValuePatch);
            }

            _logger.LogDebug($"Update the {targetTemperaturePropertyName} property under component {componentName} on the {_digitalTwinId} digital twin to {desiredTargetTemperature}.");
            HttpOperationHeaderResponse<DigitalTwinUpdateHeaders> updateDigitalTwinResponse = await _digitalTwinClient.UpdateDigitalTwinAsync(_digitalTwinId, updateOperation.Serialize());

            _logger.LogDebug($"Update {_digitalTwinId} digital twin response: {updateDigitalTwinResponse.Response.StatusCode}.");

            // Print the TemperatureController digital twin
            await GetAndPrintDigitalTwinAsync<TemperatureControllerTwin>();
        }

        private async Task InvokeRebootCommandAsync()
        {
            int delay = 1;
            const string rebootCommandName = "reboot";

            _logger.LogDebug($"Invoke the {rebootCommandName} command on the {_digitalTwinId} digital twin." +
                $"\nThis will set the \"targetTemperature\" on \"Thermostat\" component to 0.");

            HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await _digitalTwinClient.InvokeCommandAsync(
                _digitalTwinId,
                rebootCommandName,
                JsonConvert.SerializeObject(delay));

            _logger.LogDebug($"Command {rebootCommandName} was invoked on the {_digitalTwinId} digital twin." +
                $"\nDevice returned status: {invokeCommandResponse.Body.Status}. \nReport: {invokeCommandResponse.Body.Payload}");
        }

        private async Task InvokeGetMaxMinReportCommandAsync()
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

            _logger.LogDebug($"Command {getMaxMinReportCommandName} was invoked on component {componentName}." +
                $"\nDevice returned status: {invokeCommandResponse.Body.Status}. \nReport: {invokeCommandResponse.Body.Payload}");
        }
    }
}
