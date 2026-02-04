namespace BrandVue.Services.Llm;

public class ChatCompletionMessage
{
    public ChatRole Role { get; set; }
    public string Content { get; set; }

    public static ChatCompletionMessage User(string content) => new ChatCompletionMessage
    {
        Role = ChatRole.User,
        Content = content
    };

    public static ChatCompletionMessage Assistant(string content) => new ChatCompletionMessage
    {
        Role = ChatRole.Assistant,
        Content = content
    };

    public static ChatCompletionMessage System(string content) => new ChatCompletionMessage
    {
        Role = ChatRole.System,
        Content = content
    };
}
