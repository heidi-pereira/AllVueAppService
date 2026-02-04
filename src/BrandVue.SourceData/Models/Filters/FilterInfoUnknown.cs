namespace BrandVue.SourceData.Models.Filters
{
    public class FilterInfoUnknown : FilterInfo
    {
        public FilterInfoUnknown(string questionId, string[] questionClassIds) : base(questionId, questionClassIds)
        {
            IncludedValuesType = IncludedValuesTypeEnum.Unknown;
        }
    }
}