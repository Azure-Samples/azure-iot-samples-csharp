﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class RetryOperationHelper
    {
        private static readonly IRetryPolicy s_exponentialBackoffRetryStrategy = new ExponentialBackoff(
            retryCount: int.MaxValue,
            minBackoff: TimeSpan.FromMilliseconds(100),
            maxBackoff: TimeSpan.FromSeconds(10),
            deltaBackoff: TimeSpan.FromMilliseconds(100));

        public static async Task RetryTransientExceptionsAsync(Func<Task> asyncOperation, ILogger logger, IDictionary<Type, string> exceptionsToBeIgnored = null, IRetryPolicy retryPolicy = default)
        {
            if (retryPolicy == null)
            {
                retryPolicy = s_exponentialBackoffRetryStrategy;
            }

            int counter = 0;
            bool shouldRetry = false;
            TimeSpan retryInterval = TimeSpan.Zero;
            {
                try
                {
                    await asyncOperation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is IotHubException iotHubException && iotHubException.IsTransient)
                    {
                        logger.LogWarning($"An IotHubException was caught, but will try to recover and retry: {ex}");
                    }
                    else if (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        logger.LogWarning($"A network related exception was caught, but will try to recover and retry: {ex}");
                    }
                    else if (exceptionsToBeIgnored != null && exceptionsToBeIgnored.ContainsKey(ex.GetType()))
                    {
                        string exceptionLoggerMessage = exceptionsToBeIgnored[ex.GetType()];
                        logger.LogWarning($"{exceptionLoggerMessage}, ignoring : {ex}");
                    }
                    else
                    {
                        throw;
                    }

                    shouldRetry = retryPolicy.ShouldRetry(++counter, ex, out retryInterval);
                    logger.LogWarning($"Attempt {counter}: request not accepted: {ex}");
                }

                if (shouldRetry && retryInterval != TimeSpan.Zero)
                {
                    logger.LogInformation($"Will retry operation in {retryInterval}.");
                    await Task.Delay(retryInterval).ConfigureAwait(false);
                }
            }
            while (shouldRetry);
        }
    }
}
