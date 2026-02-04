using Newtonsoft.Json;

namespace BrandVue.SourceData.LlmInsights
{
    public class LlmInsightsSegment(int segmentId, string title, string insight, int significance, ICollection<LlmInsightsRelatedHeadline> relatedHeadlines)
    {
        [JsonProperty("SegmentId")]
        public int SegmentId { get; init; } = segmentId;
        [JsonProperty("Title")]
        public string Title { get; init; } = title;
        [JsonProperty("Insight")]
        public string Insight { get; init; } = insight;
        [JsonProperty("Significance")]
        public int Significance { get; init; } = significance;
        [JsonProperty("RelatedHeadlines")]
        public ICollection<LlmInsightsRelatedHeadline> RelatedHeadlines { get; init; } = relatedHeadlines;
    }
}
