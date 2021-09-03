// Copyright (c) Microsoft. All rights reserved.
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
            bool shouldRetry;
            Exception lastException;
            do
            {
                try
                {
                    await asyncOperation().ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (lastException is IotHubException iotHubException && iotHubException.IsTransient)
                    {
                        logger.LogWarning($"An IotHubException was caught, but will try to recover and retry: {lastException}");
                    }
                    else if (ExceptionHelper.IsNetworkExceptionChain(lastException))
                    {
                        logger.LogWarning($"A network related exception was caught, but will try to recover and retry: {lastException}");
                    }
                    else if (exceptionsToBeIgnored != null && exceptionsToBeIgnored.ContainsKey(lastException.GetType()))
                    {
                        string exceptionLoggerMessage = exceptionsToBeIgnored[lastException.GetType()];
                        logger.LogWarning($"{exceptionLoggerMessage}, ignoring : {lastException}");
                    }
                    else
                    {
                        throw;
                    }
                }

                shouldRetry = retryPolicy.ShouldRetry(++counter, lastException, out TimeSpan retryInterval);
                logger.LogWarning($"Attempt {counter}: request not accepted: {lastException}");

                if (shouldRetry)
                {
                    logger.LogInformation($"Will retry operation in {retryInterval}.");
                    await Task.Delay(retryInterval).ConfigureAwait(false);
                }
            }
            while (shouldRetry);
        }
    }
}
