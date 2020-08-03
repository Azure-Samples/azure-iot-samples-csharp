// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public static class ColorConsoleLoggerExtensions
    {
        public static ILoggerFactory AddColorConsoleLogger(this ILoggerFactory loggerFactory, ColorConsoleLoggerConfiguration config)
        {
            loggerFactory.AddProvider(new ColorConsoleLoggerProvider(config));
            return loggerFactory;
        }
        public static ILoggerFactory AddColorConsoleLogger(this ILoggerFactory loggerFactory)
        {
            var config = new ColorConsoleLoggerConfiguration();
            return loggerFactory.AddColorConsoleLogger(config);
        }
        public static ILoggerFactory AddColorConsoleLogger(this ILoggerFactory loggerFactory, Action<ColorConsoleLoggerConfiguration> configure)
        {
            var config = new ColorConsoleLoggerConfiguration();
            configure(config);
            return loggerFactory.AddColorConsoleLogger(config);
        }
    }
}
