namespace BrandVue.SourceData.Models.Filters
{
    public class FilterInfoNotNull : FilterInfo
    {
        public FilterInfoNotNull(string questionId, string[] questionClassIds): base(questionId, questionClassIds)
        {
            IncludedValuesType = IncludedValuesTypeEnum.NotNull;
        }
    }
}