using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Utils;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Utils
{
    [TestFixture, Timeout(2000)]
    public class ResourceLockQueueTests
    {
        private ResourceLockQueue _sut;

        [SetUp]
        public void SetUp() => _sut = new ResourceLockQueue();

        [Test]
        public async Task CanAcquireReleaseAndReacquiresLock_GivenDuplicateResourceId()
        {
            using (await _sut.WaitForDisposableLocksAsync([1, 1], CancellationToken.None))
            {
            }

            using (await _sut.WaitForDisposableLocksAsync([1, 1], CancellationToken.None))
            {
            }
        }

        [Test]
        public async Task WaitForLocksAsync_GivenMultipleCalls_AcquiresLocksSequentially()
        {
            var lockIds = new[] { 1, 2, 3 };
            var tasks = new List<Task<IDisposable>>();

            for (var i = 0; i < 5; i++)
            {
                tasks.Add(_sut.WaitForDisposableLocksAsync(lockIds, CancellationToken.None));
            }

            while (tasks.Any())
            {
                var lockAcquiredTask = await Task.WhenAny(tasks);
                using var t = await lockAcquiredTask;
                tasks.Remove(lockAcquiredTask);
                Assert.That(tasks.All(t => !t.IsCompleted));
            }
        }

        [Test]
        public async Task WaitForLocksAsync_GivenCancellation_ThrowsOperationCancelledException_AndCanReacquireLockAfterwards()
        {
            var lockIds = new[] { 1, 2, 3 };
            var cts = new CancellationTokenSource();
            using (var _ = await _sut.WaitForDisposableLocksAsync(lockIds.Take(1), CancellationToken.None))
            {
                Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    var waitForLocksAsync = _sut.WaitForDisposableLocksAsync(lockIds.Take(2), cts.Token);
                    cts.Cancel();
                    await waitForLocksAsync;
                });
                // The fact this assertion must complete while the existing lock is held ensures the cancellation isn't held up by putting things back in the right state for others.
            }
            using var task = await _sut.WaitForDisposableLocksAsync(lockIds, CancellationToken.None);
        }

        [Test]
        public async Task WaitForLocksAsync_ConcurrentRequestsForSameResource_ShouldNotGrantLocksSimultaneously()
        {
            int resourceId = 1;
            bool[] lockAcquired = { false, false };

            Task task1 = Task.Run(async () =>
            {
                using (await _sut.WaitForDisposableLocksAsync(new[] { resourceId }, CancellationToken.None))
                {
                    lockAcquired[0] = true;
                    await Task.Delay(100);
                    Assert.That(lockAcquired[1], Is.False);
                    lockAcquired[0] = false;
                }
            });

            Task task2 = Task.Run(async () =>
            {
                using (await _sut.WaitForDisposableLocksAsync(new[] { resourceId }, CancellationToken.None))
                {
                    lockAcquired[1] = true;
                    // Simulate some work
                    await Task.Delay(100);
                    Assert.That(lockAcquired[0], Is.False);
                    lockAcquired[1] = false;
                }
            });

            await Task.WhenAll(task1, task2);
        }

        [Test]
        public async Task WaitForLocksAsync_ConcurrentRequestsForDifferenceResources_ShouldGrantLocksSimultaneously()
        {
            var task1InProgress = new TaskCompletionSource();
            var task2InProgress = new TaskCompletionSource();
            Task task1 = Task.Run(async () =>
            {
                using (await _sut.WaitForDisposableLocksAsync([1], CancellationToken.None))
                {
                    task1InProgress.SetResult();
                    await task2InProgress.Task;
                }
            });

            Task task2 = Task.Run(async () =>
            {
                using (await _sut.WaitForDisposableLocksAsync([2], CancellationToken.None))
                {
                    task2InProgress.SetResult();
                    await task1InProgress.Task;
                }
            });

            await Task.WhenAll(task1, task2);
        }
    }
}