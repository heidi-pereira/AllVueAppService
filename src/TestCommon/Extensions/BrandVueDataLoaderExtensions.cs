using BrandVue.SourceData.Import;

namespace TestCommon.Extensions
{
    public static class BrandVueDataLoaderExtensions
    {
        public static void LoadBrandVueMetadataAndData(this IBrandVueDataLoader brandVueDataLoader)
        {
            brandVueDataLoader.LoadBrandVueMetadata();
            brandVueDataLoader.LoadBrandVueData();
            foreach (var subset in brandVueDataLoader.SubsetRepository)
            {
                brandVueDataLoader.RespondentRepositorySource.GetForSubset(subset);
            }
        }
    }
}