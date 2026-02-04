using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Subsets
{
    public interface ISubsetConfigurationRepository
    {
        IReadOnlyCollection<SubsetConfiguration> GetAll();
        SubsetConfiguration Create(SubsetConfiguration subsetConfiguration, string identifier);
        void Update(SubsetConfiguration subsetConfiguration, int id);
        void Delete(int subsetId);
    }
}