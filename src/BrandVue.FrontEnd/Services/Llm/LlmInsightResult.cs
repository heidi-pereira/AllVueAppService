using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.Services.Llm;

public class LlmInsightResult
{
    [Required]
    [Description("Five word or less title for the insight")]
    public string Title { get; init; }

    [Required]
    [Description("A brief paragraph on this marketing insight.")]
    public string Insight { get; init; }

    [Required]
    [Range(1, 10)]
    [Description(
        "How important the insight is with 1 being not worth talking about, and 10 being a once in a lifetime occurrence")]
    public int Significance { get; init; }

    [Description("Details of news article to be verified via news API search")]
    public ICollection<RelatedHeadline> RelatedHeadlines { get; init; }
}

public class RelatedHeadline
{
    [Description("Headline of the news article")]
    public string Headline { get; init; }

    [Description("YYYY-MM-DD Date of the news article")]
    public string Date { get; init; }

    [Description("Organisation reporting this information")]
    public string Source { get; init; }
}