using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Utils
{
    [TestFixture, Timeout(1000)]
    public class AsyncParallelQueryTests
    {
        [Test]
        public async Task SelectAwait_SingleElement_ReturnsElement()
        {
            var source = new[] { 1 };
            var asyncParallelQuery = source.AsAsyncParallel().SelectAwait(DoubleIt, CancellationToken.None);

            var results = await asyncParallelQuery.ToListAsync();

            Assert.That(results, Is.EqualTo(new List<int> { 2 }));
        }

        [Test]
        public async Task SelectAwait_MultipleElements_ReturnsProcessedElements()
        {
            var source = new[] { 1, 2, 3 };

            var asyncParallelQuery = source.AsAsyncParallel().SelectAwait(DoubleIt, CancellationToken.None);

            var results = await asyncParallelQuery.ToListAsync();

            Assert.That(results.ToList(), Is.EquivalentTo(new List<int> { 2, 4, 6 }));
        }

        [Test]
        public async Task SelectAwait_WithOrder_ReturnsOrderedResults()
        {
            var source = new[] { 3, 1, 2 };

            var asyncParallelQuery = source.AsAsyncParallel().AsOrdered().SelectAwait(DoubleIt, CancellationToken.None);

            var results = await asyncParallelQuery.ToListAsync();

            Assert.That(results, Is.EqualTo(new List<int> { 6, 2, 4 }));
        }

        [Test]
        public async Task SelectAwait_ExceptionThrown_StopsFurtherProcessing()
        {
            var source = new[] { 1, 2, 3 };
            async ValueTask<int> Selector(int x) => x == 2 ? throw new InvalidOperationException() : x * 2;

            var asyncParallelQuery = source.AsAsyncParallel().SelectAwait(Selector, CancellationToken.None);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await asyncParallelQuery.ToListAsync());
        }

        [Test]
        public async Task SelectAwait_CancellationRequested_StopsProcessing()
        {
            var source = new[] { 1, 2, 3 };
            var taskCompletionSource = new TaskCompletionSource();
            async ValueTask<int> Selector(int x)
            {
                await taskCompletionSource.Task; // Simulate some delay
                return x * 2;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var asyncParallelQuery = source.AsAsyncParallel()
                .SelectAwait(Selector, cancellationTokenSource.Token);

            cancellationTokenSource.CancelAfter(50); // Cancel after 50ms

            Assert.That(async () => await asyncParallelQuery.ToListAsync(), Throws.InstanceOf<OperationCanceledException>());
            taskCompletionSource.SetResult();
        }

        [Test]
        public async Task SelectAwait_WithOrder_StartsReturningBeforeAllCompleted()
        {
            var source = new[] { 1, 2, 3 };
            var taskCompletionSources = new[]
            {
                new TaskCompletionSource<int>(),
                new TaskCompletionSource<int>(),
                new TaskCompletionSource<int>()
            };

            async ValueTask<int> Selector(int x)
            {
                var tcs = taskCompletionSources[x - 1];
                return await tcs.Task;
            }

            var asyncParallelQuery = source.AsAsyncParallel().AsOrdered().SelectAwait(Selector, CancellationToken.None);

            var enumerator = asyncParallelQuery.GetAsyncEnumerator();

            // Complete the first task and check that it returns the result
            taskCompletionSources[0].SetResult(2);
            Assert.That(await enumerator.MoveNextAsync(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo(2));

            // Complete the second task and check that it returns the result
            taskCompletionSources[1].SetResult(4);
            Assert.That(await enumerator.MoveNextAsync(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo(4));

            // Complete the third task and check that it returns the result
            taskCompletionSources[2].SetResult(6);
            Assert.That(await enumerator.MoveNextAsync(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo(6));

            // Ensure no more results
            Assert.That(await enumerator.MoveNextAsync(), Is.False);
        }

        [Test]
        public async Task SelectAwait_LargeInputSet_CompletesSuccessfully()
        {
            var source = Enumerable.Range(1, 1000).ToList();
            var asyncParallelQuery = source.AsAsyncParallel().SelectAwait(DoubleIt, CancellationToken.None);

            var results = await asyncParallelQuery.ToListAsync();

            Assert.That(results.Count, Is.EqualTo(1000));
            Assert.That(results, Is.EquivalentTo(source.Select(x => x * 2)));
        }

        [Test]
        public async Task SelectAwait_EmptySource_ReturnsEmptyResults()
        {
            var source = Array.Empty<int>();
            var asyncParallelQuery = source.AsAsyncParallel().SelectAwait(DoubleIt, CancellationToken.None);

            var results = await asyncParallelQuery.ToListAsync();

            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task SelectAwait_StringType_ReturnsProcessedElements()
        {
            var source = new[] { "a", "bb", "ccc" };
            async ValueTask<int> StringLengthSelector(string s) => s.Length;

            var asyncParallelQuery = source.AsAsyncParallel().SelectAwait(StringLengthSelector, CancellationToken.None);

            var results = await asyncParallelQuery.ToListAsync();

            Assert.That(results, Is.EquivalentTo(new List<int> { 1, 2, 3 }));
        }

        private async ValueTask<int> DoubleIt(int x)
        {
            return x * 2;
        }
    }
}
