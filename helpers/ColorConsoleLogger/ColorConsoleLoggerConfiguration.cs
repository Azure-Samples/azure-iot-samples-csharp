// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Logging
{
    /// <summary>
    /// A color console logger configuration that creates different color console entries per log level, sets the default log level to Information and logs all events.
    /// </summary>
    public class ColorConsoleLoggerConfiguration
    {
        // If the EventId is set to 0, the logger will log all events.
        private const int DefaultEventId = 0;

        /// <summary>
        /// A dictionary containing the log level to console color mappings to be used while writing log entries to the console.
        /// </summary>
        public IReadOnlyDictionary<LogLevel, ConsoleColor> LogLevelToColorMapping { get; } = new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, ConsoleColor.Blue },
            { LogLevel.Debug, ConsoleColor.DarkYellow },
            { LogLevel.Information, ConsoleColor.Cyan },
            { LogLevel.Warning, ConsoleColor.DarkMagenta },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.DarkRed },
        };

        /// <summary>
        /// A dictionary containing the event Id to console color mappings for Azure IoT sdk events.
        /// </summary>
        public IReadOnlyDictionary<EventId, ConsoleColor> AzureIotSdkEventToColorMapping { get; } = new Dictionary<EventId, ConsoleColor>
        {
            // Microsoft.Azure.Device.Client.Enter
            { 1, ConsoleColor.Green },
            // Microsoft.Azure.Device.Client.Exit
            { 2, ConsoleColor.Green},
            // Microsoft.Azure.Device.Client.Associate
            { 3, ConsoleColor.Yellow},
            // Microsoft.Azure.Device.Client.Info
            { 4, ConsoleColor.DarkCyan},
            // Microsoft.Azure.Device.Client.Error
            { 5, ConsoleColor.Red},
            // Microsoft.Azure.Device.Client.CriticalFailure
            { 6, ConsoleColor.DarkRed},
            // Microsoft.Azure.Device.Client.Create
            { 20, ConsoleColor.Magenta},
            // Microsoft.Azure.Device.Client.GenerateToken
            { 21, ConsoleColor.Magenta}
        };

        /// <summary>
        /// The min log level that will be written to the console, defaults to <see cref="LogLevel.Information"/>.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// The list of event Ids to be written to the console. By default, all event Ids are written.
        /// </summary>
        public IEnumerable<int> EventIds { get; } = new int[] { DefaultEventId };
    }
}
