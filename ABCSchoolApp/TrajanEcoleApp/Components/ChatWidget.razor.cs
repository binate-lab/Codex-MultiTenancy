using TrajanEcole.Shared.Library.Models.Requests.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace TrajanEcoleApp.Components
{
    public partial class ChatWidget : IDisposable
    {
        private bool _isOpen;
        private bool _isLoading;
        private string _input = string.Empty;
        private readonly List<ChatMessageDto> _messages = [];

        // Mode « ancre » : la bulle n'est plus flottante (position: fixed) mais logee DANS
        // son conteneur (position: absolute), pour l'inserer dans la zone vide sous « Point
        // de l'eleve » de la page /scolarites. Defaut = false (bulle flottante classique).
        [Parameter] public bool Anchored { get; set; }

        // Vrai quand on est sur la page des versements (/scolarites).
        private bool EstSurScolarites =>
            _navigation.ToBaseRelativePath(_navigation.Uri).TrimStart('/')
                .StartsWith("scolarites", StringComparison.OrdinalIgnoreCase);

        // La bulle FLOTTANTE globale (vit dans le layout, Anchored=false) s'efface sur
        // /scolarites : c'est la version ANCREE de la page (Anchored=true) qui prend le
        // relais, dans la zone vide. Partout ailleurs, seule la flottante s'affiche.
        private bool DoitAfficher => Anchored || !EstSurScolarites;

        // Style de la bulle (bouton rond) : ancree dans le conteneur, ou flottante a droite.
        private string StyleBouton => Anchored
            ? "position: absolute; bottom: 40px; left: 12px;"
            : "position: fixed; bottom: 24px; right: 24px;";

        // Style du panneau ouvert : ancre au-dessus de la bulle (peut deborder a droite,
        // z-index eleve), ou flottant a droite comme avant.
        private string StylePanneau => Anchored
            ? "position: absolute; bottom: 108px; left: 12px; width: 360px; max-width: calc(100vw - 32px); height: 460px; max-height: 68vh;"
            : "position: fixed; bottom: 90px; right: 24px; width: 360px; max-width: calc(100vw - 32px); height: 520px; max-height: calc(100vh - 130px);";

        // La bulle vit dans le layout (montee une fois) : on re-rend a chaque navigation
        // pour reevaluer sa position quand on entre/sort de /scolarites.
        protected override void OnInitialized()
            => _navigation.LocationChanged += OnLocationChanged;

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
            => InvokeAsync(StateHasChanged);

        public void Dispose()
            => _navigation.LocationChanged -= OnLocationChanged;

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
