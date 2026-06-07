using TrajanEcole.Shared.Library.Models.Requests.Chat;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace TrajanEcoleApp.Components
{
    public partial class ChatWidget
    {
        private bool _isOpen;
        private bool _isLoading;
        private string _input = string.Empty;
        private readonly List<ChatMessageDto> _messages = [];

        private void Toggle()
        {
            _isOpen = !_isOpen;
        }

        private async Task OnKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !args.ShiftKey)
            {
                await SendAsync();
            }
        }

        private async Task SendAsync()
        {
            if (_isLoading || string.IsNullOrWhiteSpace(_input))
            {
                return;
            }

            var userText = _input.Trim();
            _input = string.Empty;

            _messages.Add(new ChatMessageDto { Role = "user", Content = userText });
            _isLoading = true;

            try
            {
                var request = new ChatRequest
                {
                    Messages = _messages
                        .Select(m => new ChatMessageDto { Role = m.Role, Content = m.Content })
                        .ToList()
                };

                var response = await _chatService.SendAsync(request);

                if (response.IsSuccessful && response.Data is not null)
                {
                    _messages.Add(new ChatMessageDto
                    {
                        Role = "assistant",
                        Content = response.Data.Reply
                    });
                }
                else
                {
                    var error = response.Messages.Count > 0
                        ? response.Messages[0]
                        : "Une erreur est survenue.";
                    _snackbar.Add(error, Severity.Error);
                    _messages.Add(new ChatMessageDto
                    {
                        Role = "assistant",
                        Content = "Désolé, je n'ai pas pu répondre pour le moment."
                    });
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Assistant indisponible : {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
