// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A color console logger configuration that sets the default level to Debug and the color to Cyan.
    /// It also logs all events.
    /// </summary>
    public class ColorConsoleLoggerConfiguration
    {
        // If the EventId is set to 0, we will log all events.
        private const int EventIdAllEvents = 0;

        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        public int EventId { get; set; } = EventIdAllEvents;
        public ConsoleColor Color { get; set; } = ConsoleColor.Cyan;
    }
}
