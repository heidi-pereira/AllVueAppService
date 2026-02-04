namespace BrandVue.Services.Llm.OpenAiCompatible;

public class OpenAiCompatibleChatServiceSettings
{
    public string ApiKey { get; set; }
    public string Model { get; set; }
    public string BaseUrl { get; set; }
    public string SummaryPromptId { get; set; }
    public bool PortkeyNoTracking { get; set; }
}