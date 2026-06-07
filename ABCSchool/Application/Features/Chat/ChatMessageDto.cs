namespace Application.Features.Chat
{
    public class ChatMessageDto
    {
        // "user" ou "assistant"
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
