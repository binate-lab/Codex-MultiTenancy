using TrajanEcole.Shared.Library.Models.Requests.Chat;
using TrajanEcole.Shared.Library.Models.Responses.Chat;
using TrajanEcole.Shared.Library.Wrappers;
using App.Infrastructure.Extensions;
using App.Infrastructure.Services.Chat;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Chat
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public ChatService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<IResponseWrapper<ChatResponse>> SendAsync(ChatRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.ChatEndpoints.Send, request);
            return await response.WrapToResponse<ChatResponse>();
        }
    }
}
