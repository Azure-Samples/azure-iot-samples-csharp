// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ThermostatSample
    {
        private const string TargetTemperatureProperty = "targetTemperature";

        // The default reported "value", "ad" and "av" on the client initial startup.
        // See https://docs.microsoft.com/en-us/azure/iot-develop/concepts-convention#writable-properties for more details in acknowledgment responses.
        private const string PropertyInitialAckDescription = "Initialized with default value";
        private const long PropertyInitialAckVersion = 0L;

        private static readonly Random s_random = new Random();
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private double _temperature = 0d;
        private double _maxTemp = 0d;

        // Dictionary to hold the temperature updates sent over.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<DateTimeOffset, double> _temperatureReadingsDateTimeOffset = new Dictionary<DateTimeOffset, double>();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        // A safe initial value for caching the writable properties version is set to 1.
        // This is so that the client will process all previous property change requests and initialize the device application.
        // With each writable property acknowledgement the version will be updated to the version received from service
        // so we have a high water mark of which version number has been processed.
        private static long s_localWritablePropertiesVersion = 1;

        public ThermostatSample(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient), $"{nameof(deviceClient)} cannot be null.");
            _logger = logger ?? LoggerFactory.Create(builer => builer.AddConsole()).CreateLogger<ThermostatSample>();
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Set handler to receive and respond to connection status changes.
            _deviceClient.SetConnectionStatusChangesHandler(async (status, reason) =>
            {
                _logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");

                // Call GetWritablePropertiesAndHandleChangesAsync() to get writable properties from the server once the connection status changes into Connected.
                // This can get back "lost" property updates in a device reconnection from status Disconnected_Retrying or Disconnected.
                if (status == ConnectionStatus.Connected)
                {
                    ClientProperties clientProperties = await _deviceClient.GetClientPropertiesAsync();
                    await HandlePropertyUpdatesAsync(clientProperties.WritablePropertyRequests);
                }
            });

            // Set handler to receive and respond to writable property update requests.
            _logger.LogDebug($"Subscribe to writable property updates.");
            await _deviceClient.SubscribeToWritablePropertyUpdateRequestsAsync(HandlePropertyUpdatesAsync, cancellationToken);

            // Set handler to receive and respond to commands.
            _logger.LogDebug($"Subscribe to commands.");
            await _deviceClient.SubscribeToCommandsAsync(HandleCommandsAsync, cancellationToken);

            // Check if the device properties (both writable and reported) are empty on the initial startup. If so, report the default values with acknowledgement to the hub.
            await CheckEmptyProperties<double>(TargetTemperatureProperty, cancellationToken);

            bool temperatureReset = true;

            // Periodically send "temperature" over telemetry.
            // Send "maxTempSinceLastReboot" over property update, when a new max temperature is reached.
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                    _temperature = GenerateTemperatureWithinRange(45, 5);
                    temperatureReset = false;
                }

                // Send temperature updates over telemetry and the value of max temperature since last reboot over property update.
                await SendTemperatureAsync();

                await Task.Delay(s_sleepDuration, cancellationToken);
            }
        }

        // The callback to handle property update requests.
        private async Task HandlePropertyUpdatesAsync(WritableClientPropertyCollection writableProperties)
        {
            long serverWritablePropertiesVersion = writableProperties.Version;

            // Check if the writable property version is outdated on the local side.
            // If the writable property update request is at a higher version than the local version, then these update requests need to be responded to.
            // For the purpose of this sample, we'll only check the writable property versions between local and server
            // side without comparing the property values.
            if (serverWritablePropertiesVersion > s_localWritablePropertiesVersion)
            {
                _logger.LogDebug($"Responding to writable property update request from version {s_localWritablePropertiesVersion} to version {serverWritablePropertiesVersion}.");

                foreach (WritableClientProperty writableProperty in writableProperties)
                {
                    switch (writableProperty.PropertyName)
                    {
                        case TargetTemperatureProperty:
                            if (writableProperties.TryGetWritableClientProperty(TargetTemperatureProperty, out WritableClientProperty targetTemperatureWritableProperty))
                            {
                                if (targetTemperatureWritableProperty.TryGetValue(out double targetTemperature))
                                {
                                    _logger.LogDebug($"Property: Received - [ \"{TargetTemperatureProperty}\": {targetTemperature}°C ].");

                                    _temperature = targetTemperature;

                                    var reportedProperty = new ClientPropertyCollection();
                                    reportedProperty.AddWritableClientPropertyAcknowledgement(targetTemperatureWritableProperty.AcknowledgeWith(CommonClientResponseCodes.OK, "Reached target temperature"));

                                    ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperty);

                                    _logger.LogDebug($"Property: Update - {reportedProperty.GetSerializedString()} is {nameof(CommonClientResponseCodes.OK)} " +
                                        $"with a version of {updateResponse.Version}.");
                                }
                                else
                                {
                                    _logger.LogWarning($"Property: Received an unrecognized value type from service:\n[ {writableProperty.PropertyName}: {writableProperty.Value} ].");
                                }
                            }

                            break;

                        default:
                            _logger.LogWarning($"Property: Received an unrecognized property update from service:\n[ {writableProperty.PropertyName}: {writableProperty.Value} ].");
                            break;
                    }
                }
            }

            s_localWritablePropertiesVersion = serverWritablePropertiesVersion;
            _logger.LogDebug($"The writable property version on local is currently {s_localWritablePropertiesVersion}.");
        }

        // The callback to handle command invocation requests.
        private Task<CommandResponse> HandleCommandsAsync(CommandRequest commandRequest)
        {
            // In this approach, we'll switch through the command name returned and handle each top-level command.
            switch (commandRequest.CommandName)
            {
                case "getMaxMinReport":
                    if (commandRequest.TryGetPayload(out DateTimeOffset sinceInUtc))
                    {
                        _logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since " +
                            $"{sinceInUtc.LocalDateTime}.");

                        Dictionary<DateTimeOffset, double> filteredReadings = _temperatureReadingsDateTimeOffset
                            .Where(i => i.Key > sinceInUtc)
                            .ToDictionary(i => i.Key, i => i.Value);

                        if (filteredReadings != null && filteredReadings.Any())
                        {
                            var report = new TemperatureReport
                            {
                                MaximumTemperature = filteredReadings.Values.Max<double>(),
                                MinimumTemperature = filteredReadings.Values.Min<double>(),
                                AverageTemperature = filteredReadings.Values.Average(),
                                StartTime = filteredReadings.Keys.Min(),
                                EndTime = filteredReadings.Keys.Max(),
                            };

                            _logger.LogDebug($"Command: MaxMinReport since {sinceInUtc.LocalDateTime}:" +
                                $" maxTemp={report.MaximumTemperature}, minTemp={report.MinimumTemperature}, avgTemp={report.AverageTemperature}, " +
                                $"startTime={report.StartTime.LocalDateTime}, endTime={report.EndTime.LocalDateTime}");

                            return Task.FromResult(new CommandResponse(CommonClientResponseCodes.OK, report));
                        }

                        _logger.LogDebug($"Command: No relevant readings found since {sinceInUtc.LocalDateTime}, cannot generate any report.");
                        return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                    }

                    _logger.LogError($"Command input for {commandRequest.CommandName} is invalid, cannot generate any report.");
                    return Task.FromResult(new CommandResponse(CommonClientResponseCodes.BadRequest));

                default:
                    _logger.LogWarning($"Received a command request that isn't" +
                        $" implemented - command name = {commandRequest.CommandName}");

                    return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
            }
        }

        // Send temperature updates over telemetry.
        // This also sends the value of max temperature since last reboot over property update.
        private async Task SendTemperatureAsync()
        {
            await SendTemperatureTelemetryAsync();

            double maxTemp = _temperatureReadingsDateTimeOffset.Values.Max<double>();
            if (maxTemp > _maxTemp)
            {
                _maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootPropertyAsync();
            }
        }

        // Send temperature update over telemetry.
        private async Task SendTemperatureTelemetryAsync()
        {
            const string telemetryName = "temperature";

            using var telemetryMessage = new TelemetryMessage
            {
                Telemetry = { [telemetryName] = _temperature }
            };
            await _deviceClient.SendTelemetryAsync(telemetryMessage);

            _logger.LogDebug($"Telemetry: Sent - {telemetryMessage.Telemetry.GetSerializedString()}.");
            _temperatureReadingsDateTimeOffset.Add(DateTimeOffset.Now, _temperature);
        }

        // Send temperature over reported property update.
        private async Task UpdateMaxTemperatureSinceLastRebootPropertyAsync()
        {
            const string propertyName = "maxTempSinceLastReboot";
            var reportedProperties = new ClientPropertyCollection();
            reportedProperties.AddRootProperty(propertyName, _maxTemp);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperties);

            _logger.LogDebug($"Property: Update - {reportedProperties.GetSerializedString()} is {nameof(CommonClientResponseCodes.OK)} " +
                $"with a version of {updateResponse.Version}.");
        }

        private static double GenerateTemperatureWithinRange(int max = 50, int min = 0)
        {
            return Math.Round(s_random.NextDouble() * (max - min) + min, 1);
        }

        private async Task CheckEmptyProperties<T>(string propertyName, CancellationToken cancellationToken)
        {
            ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

            WritableClientPropertyCollection writableProperty = properties.WritablePropertyRequests;
            ClientPropertyCollection reportedProperty = properties.ReportedByClient;

            // Check if the device properties are empty.
            if (!writableProperty.Contains(propertyName) && !reportedProperty.Contains(propertyName))
            {
                await ReportInitialProperty<T>(propertyName, cancellationToken);
            }
        }

        private async Task ReportInitialProperty<T>(string propertyName, CancellationToken cancellationToken)
        {
            var reportedProperties = new ClientPropertyCollection();

            // Report the default value with acknowledgement(ac=203, av=0) as part of the PnP convention.
            // "DefaultPropertyValue" is set from the device when the desired property is not set via the hub.

            T propertyValue = default;
            var writableClientPropertyAcknowledgement = new WritableClientPropertyAcknowledgement
            {
                PropertyName = propertyName,
                Payload = _deviceClient.PayloadConvention.PayloadSerializer.CreateWritablePropertyAcknowledgementPayload(
                            propertyValue,
                            ClientResponseCodes.DeviceInitialProperty,
                            PropertyInitialAckVersion,
                            PropertyInitialAckDescription),
            };

            reportedProperties.AddWritableClientPropertyAcknowledgement(writableClientPropertyAcknowledgement);
            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(reportedProperties, cancellationToken);

            _logger.LogDebug($"Report the default value with acknowledgement on the client initial startup.\nProperty: Update - {reportedProperties.GetSerializedString()} " +
                $"is {nameof(CommonClientResponseCodes.OK)} with a version of {updateResponse.Version}.");
        }
    }
}
