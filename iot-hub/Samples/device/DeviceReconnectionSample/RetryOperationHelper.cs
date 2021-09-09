﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A helper class with methods that aid in retrying operations.
    /// </summary>
    internal class RetryOperationHelper
    {
        private static readonly IRetryPolicy s_exponentialBackoffRetryStrategy = new ExponentialBackoff(
            retryCount: int.MaxValue,
            minBackoff: TimeSpan.FromMilliseconds(100),
            maxBackoff: TimeSpan.FromSeconds(10),
            deltaBackoff: TimeSpan.FromMilliseconds(100));

        /// <summary>
        /// Retry an async operation based on the retry strategy supplied.
        /// </summary>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="isClientConnected">A function that determines if the client is currently connected. Operations are retried only when the client is connected.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance to be used.</param>
        /// <param name="exceptionsToBeIgnored">The list of exceptions that can be ignored.</param>
        /// <param name="retryPolicy">The retry policy to be applied.</param>
        internal static async Task RetryTransientExceptionsAsync(
            Func<Task> asyncOperation,
            Func<bool> isClientConnected,
            ILogger logger,
            IDictionary<Type, string> exceptionsToBeIgnored = default,
            IRetryPolicy retryPolicy = default)
        {
            if (retryPolicy == null)
            {
                retryPolicy = s_exponentialBackoffRetryStrategy;
            }

            int counter = 0;
            bool shouldRetry;
            do
            {
                Exception lastException = new IotHubCommunicationException("Client is currently reconnecting internally; attempt the operation after some time.");

                try
                {
                    if (isClientConnected())
                    {
                        await asyncOperation();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is IotHubException iotHubException && iotHubException.IsTransient)
                    {
                        logger.LogWarning($"A transient IotHubException was caught, but will try to recover and retry: {ex}");
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

                    lastException = ex;
                }

                shouldRetry = retryPolicy.ShouldRetry(++counter, lastException, out TimeSpan retryInterval);

                if (shouldRetry)
                {
                    logger.LogInformation($"Will retry operation in {retryInterval}, attempt {counter}.");
                    await Task.Delay(retryInterval);

                }
                else
                {
                    logger.LogWarning($"Retry policy determined that the operation should no longer be retried, stopping retries.");
                }
            }
            while (shouldRetry);
        }
    }
}
