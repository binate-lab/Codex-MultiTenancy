using Application.Wrappers;
using MediatR;

namespace Application.Features.Chat.Commands
{
    public class SendChatMessageCommand : IRequest<IResponseWrapper>
    {
        public ChatRequest ChatRequest { get; set; }
    }

    public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, IResponseWrapper>
    {
        private readonly IChatService _chatService;

        public SendChatMessageCommandHandler(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<IResponseWrapper> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
        {
            var response = await _chatService.SendMessageAsync(request.ChatRequest, cancellationToken);
            return await ResponseWrapper<ChatResponse>.SuccessAsync(response);
        }
    }
}
