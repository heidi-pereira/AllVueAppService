namespace BrandVue.SourceData.LazyLoading
{
    public class EntityMetricData
    {
        public int ResponseId { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int? SurveyId { get; set; }
        public EntityIds EntityIds { get; set; }
        public List<(ResponseFieldDescriptor Field, int Value)> Measures { get; set; } = new List<(ResponseFieldDescriptor Field, int Value)>();
        public List<(ResponseFieldDescriptor Field, string Value)> TextFields { get; set; } = new List<(ResponseFieldDescriptor Field, string Value)>();
    }
}