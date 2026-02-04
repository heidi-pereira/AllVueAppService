namespace BrandVue.SourceData.LazyLoading
{
    public interface IInvalidatableLoaderCache
    {
        void InvalidateCacheEntry(string productName, string subProductId);
        void InvalidateQuestions(IList<int[]> surveyIdsForEachSubset);
    }
}