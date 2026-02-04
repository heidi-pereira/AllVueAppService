using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace // Make it appear whenever AsParallel is available to maximise visibility
namespace System.Linq; 

public static class AsyncParallelQueryExtensions
{
    /// <summary>
    /// Use this when you have a CPU bound async task that needs parallelism.
    /// </summary>
    /// <remarks>Like ForEachAsync, this bounds parallelism.
    /// Use *instead* of ~Task.WhenAll(source.AsParallel())~ - which will cause too many threads to spawn for large inputs
    /// </remarks>
    public static AsyncParallelQuery<TSource> AsAsyncParallel<TSource>(this IEnumerable<TSource> source) => new(source, false);

    /// <summary>Use this when you have a CPU bound async task that needs parallelism</summary>
    /// <remarks>
    /// Before extending this, check if there's now a built-in version of something like this
    /// </remarks>
    public class AsyncParallelQuery<TSource>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly bool _sort;

        internal AsyncParallelQuery(IEnumerable<TSource> source, bool sort)
        {
            _source = source;
            _sort = sort;
        }

        public AsyncParallelQuery<TSource> AsOrdered() => new(_source, true);

        public IAsyncEnumerable<TResult> SelectAwait<TResult>(Func<TSource, ValueTask<TResult>> selector,
            CancellationToken cancellationToken)
        {
            if (_source.TryGetNonEnumeratedCount(out var count) && count <= 1)
            {
                return _source.ToAsyncEnumerable().SelectAwait(selector);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var parallelCancellationToken = cts.Token;

            var channel = Channel.CreateUnbounded<(TResult Result, int SourceIndex)>(new UnboundedChannelOptions()
                { SingleReader = true, SingleWriter = false });

            // Intentionally not awaited so that we can start returning results before total completion
            _ = Parallel.ForEachAsync(_source.Select((sourceItem, index) => (sourceItem, index)), parallelCancellationToken,
                async (t, c) =>
                {
                    try
                    {
                        var result = await selector(t.sourceItem);
                        await channel.Writer.WriteAsync((result, t.index), c);
                    }
                    catch (Exception e)
                    {
                        // Pass error to containing context, and stop any further processing
                        channel.Writer.Complete(e);
                        await cts.CancelAsync();
                    }
                }).ContinueWith(_ => channel.Writer.Complete(), parallelCancellationToken);

            var indexedResults = channel.Reader.ReadAllAsync(cancellationToken);

            return _sort ? OrderBySourceIndexAsync(indexedResults) : indexedResults.Select(x => x.Result);
        }

        /// <summary>
        /// Functionally equivalent to source.OrderBy(x => x.SourceIndex)
        /// But starts returning items as soon as ready for consumption upstream rather than sorting after the whole enumerable is complete
        /// </summary>
        private async IAsyncEnumerable<TResult> OrderBySourceIndexAsync<TResult>(
            IAsyncEnumerable<(TResult Result, int SourceIndex)> source)
        {
            var buffer = new Dictionary<int, TResult>();
            int nextIndex = 0;
            await foreach (var item in source)
            {
                if (item.SourceIndex == nextIndex)
                {
                    yield return item.Result;
                    nextIndex++;

                    while (buffer.Remove(nextIndex, out var result))
                    {
                        yield return result;
                        nextIndex++;
                    }
                }
                else
                {
                    buffer[item.SourceIndex] = item.Result;
                }
            }

            while (buffer.Remove(nextIndex, out var result))
            {
                yield return result;
                nextIndex++;
            }

        }
    }
}