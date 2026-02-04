namespace BrandVue.SourceData.Subsets
{
    public static class SubsetRepositoryExtension
    {
        public static IList<int[]> GetSurveyIdsForEnabledSubsets(this ISubsetRepository subsetRepo)
        {
            var result = new List<int[]>();
            foreach (var subset in subsetRepo)
            {
                if (!subset.Disabled)
                {
                    result.Add(subset.GetSurveyIdForSubset());
                }
            }
            return result;
        }

        public static int[] GetSurveyIdForSubset(this Subset subset)
        {
            return subset.SurveyIdToSegmentNames.Keys.ToArray();
        }
    }
}