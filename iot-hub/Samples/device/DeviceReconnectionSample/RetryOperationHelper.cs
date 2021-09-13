// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A helper class with methods that aid in retrying operations.
    /// </summary>
    internal class RetryOperationHelper
    {
        /// <summary>
        /// Retry an async operation on encountering a transient operation. The retry strategy followed is an exponential backoff strategy.
        /// </summary>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="shouldExecuteOperation">A function that determines if the operation should be executed.
        /// Eg.: for scenarios when we want to execute the operation only if the client is connected, this would be a function that returns if the client is currently connected.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance to be used.</param>
        /// <param name="exceptionsToBeIgnored">An optional list of exceptions that can be ignored.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        internal static async Task RetryTransientExceptionsAsync(
            Func<Task> asyncOperation,
            Func<bool> shouldExecuteOperation,
            ILogger logger,
            IDictionary<Type, string> exceptionsToBeIgnored = default,
            CancellationToken cancellationToken = default)
        {
            IRetryPolicy retryPolicy = new ExponentialBackoffTransientExceptionRetryPolicy(maxRetryCount: int.MaxValue, exceptionsToBeIgnored: exceptionsToBeIgnored);

            int counter = 0;
            bool shouldRetry;
            do
            {
                Exception lastException = null;
                try
                {
                    if (shouldExecuteOperation())
                    {
                        await asyncOperation();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                shouldRetry = retryPolicy.ShouldRetry(++counter, lastException, out TimeSpan retryInterval);
                if (shouldRetry)
                {
                    logger.LogInformation($"A transient recoverable exception was caught, will retry operation in {retryInterval}, attempt {counter}.");
                    await Task.Delay(retryInterval);

                }
                else
                {
                    logger.LogWarning($"Retry policy determined that the operation should no longer be retried, stopping retries.");
                }
            }
            while (shouldRetry && !cancellationToken.IsCancellationRequested);
        }
    }
}
