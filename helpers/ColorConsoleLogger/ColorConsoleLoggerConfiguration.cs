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
        internal const int DefaultEventId = 0;

        /// <summary>
        /// A dictionary containing the log level to console color mappings to be used while writing log entries to the console.
        /// </summary>
        public IReadOnlyDictionary<LogLevel, ConsoleColor> LogLevelToColorMapping { get; } = new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, ConsoleColor.Blue },
            { LogLevel.Debug, ConsoleColor.DarkYellow },
            { LogLevel.Information, ConsoleColor.Cyan },
            { LogLevel.Warning, ConsoleColor.Red },
            { LogLevel.Error, ConsoleColor.DarkRed },
            { LogLevel.Critical, ConsoleColor.DarkRed },
        };

        /// <summary>
        /// A dictionary containing the client SDK event Id to console color mappings to be used while writing log entries to the console.
        /// <list type="bullet">
        /// <item> <description>EnterEventId = 1</description> </item>
        /// <item> <description>ExitEventId = 2</description> </item>
        /// <item> <description>AssociateEventId = 3</description> </item>
        /// <item> <description>InfoEventId = 4</description> </item>
        /// <item> <description>ErrorEventId = 5</description> </item>
        /// <item> <description>CriticalFailureEventId = 6</description> </item>
        /// <item> <description>DumpArrayEventId = 7</description> </item>
        /// <item> <description>CreateId = 20</description> </item>
        /// <item> <description>GenerateTokenId = 21</description> </item>
        /// </list>
        /// </summary>
        public IReadOnlyDictionary<int, ConsoleColor> ClientSdkEventToColorMapping { get; } = new Dictionary<int, ConsoleColor>
        {
            { 1, ConsoleColor.DarkGreen },
            { 2, ConsoleColor.DarkGreen },
            { 3, ConsoleColor.Yellow },
            { 4, ConsoleColor.Blue },
            { 5, ConsoleColor.DarkMagenta },
            { 6, ConsoleColor.DarkMagenta },
            { 7, ConsoleColor.Magenta },
            { 20, ConsoleColor.Yellow },
            { 21, ConsoleColor.Green },
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
