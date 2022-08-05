// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    // This application will do the following:
    //   * Create new devices and add them to an IoT hub (for testing) -- you specify how many you want to add
    //      --> This has been tested up to 500,000 devices,
    //          but should work all the way up to the million devices allowed on a hub.
    //   * Copy the devices from one hub to another.
    //   * Delete the devices from any hub -- referred to as source or destination in case
    //       you're cloning a hub and want to test adding or copying devices more than once.
    //       This option is to clean up the hubs after the sample has finished.
    //
    // Be advised: The size of the hubs you are using should be able to manage the number of devices
    //  you want to create and test with.
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(
                    parsedParams =>
                    {
                        parameters = parsedParams;
                    })
                .WithNotParsed(
                    errors =>
                    {
                        Environment.Exit(1);
                    });

            try
            {
                // Instantiate the class and run the sample.
                var importExportDevicesSample = new ImportExportDevicesSample(
                    parameters.SourceIotHubConnectionString,
                    parameters.DestIotHubConnectionString,
                    parameters.StorageConnectionString,
                        parameters.ContainerName,
                        parameters.DevicesBlobName);

                await importExportDevicesSample
                    .RunSampleAsync(
                        parameters.AddDevices,
                        parameters.CopyDevices,
                        parameters.DeleteSourceDevices,
                        parameters.DeleteDestDevices)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error. Description = {ex.Message}");
                Console.WriteLine($"Error. Description = {ex.Message}\n{ex.StackTrace}");
            }

            Console.WriteLine("Finished. Press any key to continue.");
            Console.ReadKey(true);
        }
    }
}
