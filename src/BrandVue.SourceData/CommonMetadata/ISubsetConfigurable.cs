namespace BrandVue.SourceData.CommonMetadata
{
    public interface ISubsetConfigurable
    {
        IReadOnlyList<string> GetSubsets();
        void SetSubsets(IEnumerable<string> subsets, ISubsetRepository repository);
    }
}
