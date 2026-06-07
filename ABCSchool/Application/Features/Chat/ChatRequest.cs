namespace Application.Features.Chat
{
    public class ChatRequest
    {
        // Historique de la conversation (le dernier message est celui de l'utilisateur)
        public List<ChatMessageDto> Messages { get; set; } = [];
    }
}
