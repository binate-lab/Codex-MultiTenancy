namespace Application.Features.Chat
{
    public interface IChatService
    {
        Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken);
    }
}
