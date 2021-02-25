// Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class TaskHelper
    {
        internal static Task WhenAllFailFast(params Task[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasks.Length == 0)
            {
                return Task.CompletedTask;
            }

            tasks.ToList().ForEach(x =>
            {
                if (x is null)
                {
                    throw new ArgumentNullException(nameof(x));
                }
            });

            var results = new bool[tasks.Length];
            var remaining = tasks.Length;
            var tcs = new TaskCompletionSource<bool[]>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            for (int i = 0; i < tasks.Length; i++)
            {
                var task = tasks[i];
                HandleCompletion(task, i);
            }
            return tcs.Task;

            async void HandleCompletion(Task task, int index)
            {
                try 
                {
                    await task.ConfigureAwait(false);
                    results[index] = true;
                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        tcs.TrySetResult(results);
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
        }
    }
}
