// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ColorConsoleLoggerConfiguration
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        public int EventId { get; set; } = 0;
        public ConsoleColor Color { get; set; } = ConsoleColor.Cyan;
    }
}
