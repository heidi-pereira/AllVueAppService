using System.Collections.Generic;

namespace BrandVue.EntityFramework
{
    public interface ISubsetIdProvider
    {
        string SubsetId { get; }
    }
    public interface ISubsetIdsProvider<out TEnumerable> where TEnumerable : IEnumerable<string>
    {
        TEnumerable SubsetIds { get; }
    }
    public interface ISubsetIdsProvider : ISubsetIdsProvider<IEnumerable<string>>
    {
    }
}