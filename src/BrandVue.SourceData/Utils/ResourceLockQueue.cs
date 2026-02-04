using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.SourceData.Utils
{
    /// <summary>
    /// Manages a set of resources by id. Allows queueing up whether needing one or multiple resources exclusively.
    /// </summary>
    internal class ResourceLockQueue
    {
        private readonly Dictionary<int, Task> _queueByResourceIndex = new();

        /// <summary>
        /// Acquires locks for each given integer id safely. Releases the locks when disposed. Always use in a using block.
        /// </summary>
        /// <remarks>You must not already hold any a resource managed by this class, or any other resource that another existing user of these resources could need, or deadlock will ensue</remarks>
        public async Task<IDisposable> WaitForDisposableLocksAsync(IEnumerable<int> lockIdsEnumerable, CancellationToken cancellationToken)
        {
            var uniqueOrderedLockIds = lockIdsEnumerable.Distinct().Order();
            var tcs = new TaskCompletionSource();
            var singleUseDisposableLock = new SingleUseDisposableLock(tcs);
            Task allPreviousTasksComplete = null;
            try
            {
                var queueBehindAndReturnExistingTasks = UpdateAndReturnPreviousTasks(uniqueOrderedLockIds, tcs.Task);
                allPreviousTasksComplete = Task.WhenAll(queueBehindAndReturnExistingTasks);
                await allPreviousTasksComplete.WaitAsync(cancellationToken);
            }
            catch (Exception) //e.g. Cancellation
            {
                if (allPreviousTasksComplete?.IsCompleted == false)
                {
                    // Fire and forget so that cancelled operation can end.
                    _ = allPreviousTasksComplete.ContinueWith(_ => singleUseDisposableLock.Dispose(), CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
                }
                
                throw;
            }

            return singleUseDisposableLock;
        }

        private Task[] UpdateAndReturnPreviousTasks(IEnumerable<int> orderedLockIds, Task task)
        {
            // Coarse grained locking while updating multiple dictionary entries for simplicity
            // if this is a point of contention in the code, then it likely isn't this code at fault, the resources themselves are too coarse grained for the traffic.
            lock (_queueByResourceIndex)
            {
                Task UpdateAndReturnPrevious(int lockId)
                {
                    var hasExisting = _queueByResourceIndex.TryGetValue(lockId, out var existingTask);
                    _queueByResourceIndex[lockId] = task;
                    return hasExisting ? existingTask : null;
                }

                return orderedLockIds.Select(UpdateAndReturnPrevious).Where(x => x is { IsCompleted: false }).ToArray();
            }
        }

        private class SingleUseDisposableLock : IDisposable
        {
            private readonly TaskCompletionSource _completionSource;
            public SingleUseDisposableLock(TaskCompletionSource completionSource) => _completionSource = completionSource;
            public void Dispose()
            {
                _completionSource.SetResult();
            }
        }
    }
}