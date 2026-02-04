using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Subsets
{

    /// <remarks>
    /// Iteration is guaranteed to be a consistent order across app restarts <see cref="SubsetRepository.GetEnumeratorInternal"/>
    /// </remarks>
    public interface ISubsetRepository : IEnumerable<Subset>
    {
        //IEnumerator<Subset> GetEnumerator();
        int Count { get; }
        Subset Get(String identity);
        bool TryGet(string identity, out Subset stored);
        bool HasSubset(string subsetId);
    }
}
