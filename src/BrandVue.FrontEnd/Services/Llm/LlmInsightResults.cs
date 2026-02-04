namespace BrandVue.Services.Llm
{
    public record LlmInsightResults(string Id, LlmInsight[] AiSummary, LlmInsightUserFeedback UserFeedback);
    public record LlmInsight(int SegmentId, string Title, string Insight, int Significance, bool? UserFeedbackSegmentCorrectness, ICollection<LlmInsightRelatedHeadline> RelatedHeadlines);
    public record LlmInsightRelatedHeadline(string Headline, string Date, string Source);
    public record LlmInsightUserFeedback(DateTime Created, string? UserComment, bool? IsUseful);
}
