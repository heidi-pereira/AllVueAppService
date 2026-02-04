using Newtonsoft.Json;

namespace BrandVue.SourceData.LlmInsights
{
    public class LlmInsightsRelatedHeadline(string headline, string date, string source)
    {

        [JsonProperty("Headline")]
        public string Headline { get; init; } = headline;
        [JsonProperty("Date")]
        public string Date { get; init; } = date;
        [JsonProperty("Source")]
        public string Source { get; init; } = source;

    }
}
