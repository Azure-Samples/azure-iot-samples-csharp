// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ColorConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ColorConsoleLoggerConfiguration _config;
        private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers = new ConcurrentDictionary<string, ColorConsoleLogger>();

        public ColorConsoleLoggerProvider(ColorConsoleLoggerConfiguration config)
        {
            _config = config;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, _config));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
