// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A color console logger configuration that sets the default log level to Information and logs all events.
    /// </summary>
    public class ColorConsoleLoggerConfiguration
    {
        // If the EventId is set to 0, the logger will log all events.
        private const int DefaultEventId = 0;

        private static readonly IDictionary<LogLevel, ConsoleColor> s_defaultColorMapping = new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, ConsoleColor.Blue },
            { LogLevel.Debug, ConsoleColor.DarkYellow },
            { LogLevel.Information, ConsoleColor.Cyan },
            { LogLevel.Warning, ConsoleColor.DarkMagenta },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.DarkRed }
        };

        public ColorConsoleLoggerConfiguration()
        {
            LogLevelToColorMapping = s_defaultColorMapping;
        }

        public ColorConsoleLoggerConfiguration(IDictionary<LogLevel, ConsoleColor> customConsoleColorMapping)
        {
            var customMap = s_defaultColorMapping;

            // If a custom color mapping is provided, use it to override the default color mapping.
            foreach (KeyValuePair<LogLevel, ConsoleColor> entry in customConsoleColorMapping)
            {
                if (customMap.ContainsKey(entry.Key))
                {
                    customMap[entry.Key] = entry.Value;
                }
            }
            LogLevelToColorMapping = customMap;
        }

        public IDictionary<LogLevel, ConsoleColor> LogLevelToColorMapping { get; }

        public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

        public IEnumerable<int> EventIds { get; set; } = new List<int>() { DefaultEventId };
    }
}
