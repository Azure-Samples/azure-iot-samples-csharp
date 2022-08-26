// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample demonstrates automatic device management using device configurations.
    /// For module configurations, refer to: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/service/EdgeDeploymentSample
    /// </summary>
    public class AutomaticDeviceManagementSample
    {
        private readonly RegistryManager _registryManager;

        public AutomaticDeviceManagementSample(RegistryManager registryManager)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine("Create configurations");
            await AddDeviceConfigurationAsync("config001");
            await AddDeviceConfigurationAsync("config002");
            await AddDeviceConfigurationAsync("config003");
            await AddDeviceConfigurationAsync("config004");
            await AddDeviceConfigurationAsync("config005");

            Console.WriteLine("List existing configurations");
            await GetConfigurationsAsync(5);

            Console.WriteLine("Remove some connfigurations");
            await DeleteConfigurationAsync("config004");
            await DeleteConfigurationAsync("config002");

            Console.WriteLine("List existing configurations");
            await GetConfigurationsAsync(5);

            Console.WriteLine("Remove remaining connfigurations");
            await DeleteConfigurationAsync("config001");
            await DeleteConfigurationAsync("config003");
            await DeleteConfigurationAsync("config005");

            Console.WriteLine("List existing configurations (should be empty)");
            await GetConfigurationsAsync(5);
        }

        private async Task AddDeviceConfigurationAsync(string configurationId)
        {
            var configuration = new Configuration(configurationId);

            CreateDeviceContent(configuration, configurationId);
            CreateMetricsAndTargetCondition(configuration, configurationId);

            await _registryManager.AddConfigurationAsync(configuration);

            Console.WriteLine($"Configuration added, id: {configurationId}");
        }

        private static void CreateDeviceContent(Configuration configuration, string configurationId)
        {
            configuration.Content = new ConfigurationContent
            {
                DeviceContent = new Dictionary<string, object>()
            };
            configuration.Content.DeviceContent["properties.desired.deviceContent_key"] = "deviceContent_value-" + configurationId;
        }

        private static void CreateMetricsAndTargetCondition(Configuration configuration, string configurationId)
        {
            configuration.Metrics.Queries.Add("waterSettingsPending", "SELECT deviceId FROM devices WHERE properties.reported.chillerWaterSettings.status=\'pending\'");
            configuration.TargetCondition = "properties.reported.chillerProperties.model=\'4000x\'";
            configuration.Priority = 20;
        }

        private async Task DeleteConfigurationAsync(string configurationId)
        {
            await _registryManager.RemoveConfigurationAsync(configurationId);

            Console.WriteLine($"Configuration deleted, id: {configurationId}");
        }

        private async Task GetConfigurationsAsync(int count)
        {
            IEnumerable<Configuration> configurations = await _registryManager.GetConfigurationsAsync(count);

            // Check configuration's metrics for expected conditions
            foreach (Configuration configuration in configurations)
            {
                string configurationString = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                Console.WriteLine(configurationString);
                Thread.Sleep(1000);
            }

            Console.WriteLine("Configurations received");
        }
    }
}
