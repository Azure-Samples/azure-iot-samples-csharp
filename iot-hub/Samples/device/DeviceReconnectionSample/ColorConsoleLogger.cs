// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class ColorConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly ColorConsoleLoggerConfiguration _config;

        public ColorConsoleLogger(string name, ColorConsoleLoggerConfiguration config)
        {
            _name = name;
            _config = config;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == _config.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (_config.EventId == 0 || _config.EventId == eventId.Id)
            {
                var color = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)}>>");

                Console.ForegroundColor = _config.Color;
                Console.WriteLine($"{logLevel} - {formatter(state, exception)}");

                Console.ForegroundColor = color;
            }
        }
    }
}
