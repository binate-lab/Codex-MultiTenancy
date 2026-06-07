using TrajanEcole.Shared.Library.Models.Requests.Chat;
using TrajanEcole.Shared.Library.Models.Responses.Chat;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Chat
{
    public interface IChatService
    {
        Task<IResponseWrapper<ChatResponse>> SendAsync(ChatRequest request);
    }
}
