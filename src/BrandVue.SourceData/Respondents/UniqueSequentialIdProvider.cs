using System.Threading;

namespace BrandVue.SourceData.Respondents;

public class UniqueSequentialIdProvider
{
    private int _loadOrderIndexLatest;

    public UniqueSequentialIdProvider(int loadOrderIndexLatest = -1) => _loadOrderIndexLatest = loadOrderIndexLatest;

    public int GetNextId() => Interlocked.Increment(ref _loadOrderIndexLatest);
}