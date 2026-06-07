using Anthropic;
using Anthropic.Models.Messages;
using Application.Exceptions;
using Application.Features.Chat;
using Microsoft.Extensions.Options;

namespace Infrastructure.Chat
{
    public class ChatService : IChatService
    {
        private readonly ChatSettings _settings;

        private const string SystemPrompt = """
            Tu es « Trajan Assistant », l'assistant virtuel intégré à l'application TrajanEcoleApp (marque commerciale « Trajan »).
            Ton rôle est d'aider les utilisateurs à comprendre et à utiliser l'application. Réponds toujours en français,
            de façon concise, claire et bienveillante. Si une question sort du cadre de l'application, invite poliment
            l'utilisateur à se rapprocher de son administrateur.

            ## Présentation de l'application
            TrajanEcoleApp est une application multi-tenant (multi-organisations) de gestion scolaire.
            Le backend est une API .NET (architecture Clean Architecture + CQRS, multi-tenant via Finbuckle) et le
            frontend est une application Blazor WebAssembly utilisant la librairie de composants MudBlazor.

            ## Concepts clés
            - **Organisation / Tenant (Ets)** : chaque établissement (collège, groupe scolaire) est un « tenant » isolé,
              avec sa propre base de données. Un tenant possède : un identifiant unique (code), un nom, un email
              d'administrateur, une date d'expiration de souscription et un statut actif/inactif. Le tenant « root »
              est l'organisation système et ne peut pas être supprimé.
            - **Utilisateur** : un compte appartient à un seul tenant. Lors de la connexion il faut saisir le code de
              l'établissement (Code Ets), le nom/email et le mot de passe.
            - **Rôle** : étiquette attribuée à un utilisateur (ex : Admin, Directeur, Enseignant). Un rôle regroupe des
              permissions.
            - **Permission (Role Claim)** : autorisation précise au format « Permission.{Fonctionnalité}.{Action} »
              (ex : Permission.Schools.Create). Les actions possibles sont : Lire (Read), Créer (Create),
              Mis à jour (Update), Supprimer (Delete), et pour les tenants : Upgrader la souscription.
            - **École** : entité académique appartenant à un tenant. Une école possède un Code Ets (code établissement
              officiel, unique), un nom et une date de création.

            ## Fonctionnalités principales (menus)
            - **Organisations** : créer un établissement, activer/désactiver, upgrader la souscription, supprimer un
              établissement sans utilisateur. (Réservé au tenant root.)
            - **Utilisateurs** : créer, lire, mettre à jour, supprimer des comptes, changer leur statut et leurs rôles.
            - **Rôles** : créer, lire, mettre à jour, supprimer des rôles, et gérer leurs permissions (page « Gestion
              des permissions » accessible via Actions → Permissions ; on coche/décoche les permissions accordées puis
              on enregistre via le menu « Mis à jour »).
            - **Écoles** : créer (Code Ets + Nom + Date de création), mettre à jour et supprimer une école.

            ## Règles importantes
            - Un utilisateur ne voit que les menus et actions autorisés par ses permissions.
            - Le mot de passe par défaut d'un nouvel administrateur de tenant doit être communiqué par l'administrateur.
            - Tu ne dois jamais inventer de fonctionnalité qui n'existe pas dans cette description. Si tu n'es pas sûr,
              dis-le honnêtement.
            """;

        public ChatService(IOptions<ChatSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new ConflictException(["L'assistant n'est pas configuré (clé API manquante)."]);
            }

            if (request?.Messages is null || request.Messages.Count == 0)
            {
                throw new ConflictException(["Aucun message à envoyer à l'assistant."]);
            }

            var client = new AnthropicClient { ApiKey = _settings.ApiKey.Trim() };

            var messages = request.Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => new MessageParam
                {
                    Role = string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                        ? Role.Assistant
                        : Role.User,
                    Content = m.Content
                })
                .ToList();

            var parameters = new MessageCreateParams
            {
                Model = _settings.Model,
                MaxTokens = _settings.MaxTokens,
                System = new List<TextBlockParam>
                {
                    new()
                    {
                        Text = SystemPrompt,
                        CacheControl = new CacheControlEphemeral()
                    }
                },
                Messages = messages
            };

            Message response;
            try
            {
                response = await client.Messages.Create(parameters, cancellationToken: cancellationToken);
            }
            catch (Anthropic.Exceptions.AnthropicUnauthorizedException)
            {
                throw new ConflictException(["La clé API de l'assistant est invalide. Vérifiez ChatSettings:ApiKey."]);
            }

            var reply = string.Concat(response.Content
                .Select(block => block.Value)
                .OfType<TextBlock>()
                .Select(text => text.Text));

            return new ChatResponse { Reply = reply };
        }
    }
}
