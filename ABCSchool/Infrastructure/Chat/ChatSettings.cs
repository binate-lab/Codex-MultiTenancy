namespace Infrastructure.Chat
{
    public class ChatSettings
    {
        public string ApiKey { get; set; }
        public string Model { get; set; } = "claude-haiku-4-5";
        public int MaxTokens { get; set; } = 1024;
    }
}
