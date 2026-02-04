using JetBrains.Annotations;
using System.Runtime.Serialization;

namespace BrandVue.Models
{
    public class QueryParams
    {
        public string? Average { get; init; }
        public string? Start { get; init; }
        public string? End { get; init; }
        public ComparisonPeriodSelection? Period { get; init; }
        public string? ShowAverageIndicator { get; init; }
        public string? Metric { get; init; }
        public int? AudienceId { get; init; }
        public List<int> AudienceInstanceOrMappingIds { get; init; }
        public int? AudienceMultipleChoiceByValue { get; init; }
        public int? BaseVariableId1 { get; init; }
        public int? BaseVariableId2 { get; init; }
    }

  
    public enum ComparisonPeriodSelection
    {
        [EnumMember(Value = "Current")]
        CurrentPeriodOnly,
        [EnumMember(Value = "Previous")]
        CurrentAndPreviousPeriod,
        [EnumMember(Value = "LastSixMonths")]
        LastSixMonths,
        [EnumMember(Value = "SameLastYear")]
        SameLastYear
    }
}