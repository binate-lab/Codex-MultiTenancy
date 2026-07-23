using App.Infrastructure.Constants;
using App.Infrastructure.Services.Identity;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace App.Infrastructure.Services.Auth
{
    // Pose le Bearer sur chaque requete API et, sur 401, tente UN refresh du token
    // puis UN rejeu de la requete (le JWT etait probablement expire). Si le refresh
    // echoue, le 401 d'origine est rendu tel quel : le message honnete des pages
    // (rapports/recu) l'affichera et l'utilisateur se reconnectera.
    public class AuthenticationHeaderHandler : DelegatingHandler
    {
        // Un seul refresh a la fois (plusieurs appels paralleles peuvent prendre 401
        // en meme temps) ; WASM est monothread mais les awaits s'entrelacent.
        private static readonly SemaphoreSlim _verrouRefresh = new(1, 1);

        private readonly ILocalStorageService _storageService;
        private readonly IServiceProvider _services;

        // ITokenService est resolu PARESSEUSEMENT (au 401 seulement) : l'injecter au
        // constructeur creerait un cycle handler -> TokenService -> HttpClient -> handler.
        public AuthenticationHeaderHandler(ILocalStorageService storageService, IServiceProvider services)
        {
            _storageService = storageService;
            _services = services;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Headers.Authorization?.Scheme != "Bearer")
            {
                var savedToken = await _storageService.GetItemAsync<string>(StorageConstants.AuthToken, ct);
                if (!string.IsNullOrEmpty(savedToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
                }
            }

            var reponse = await base.SendAsync(request, ct);

            // Pas un 401, ou route token/ (login, refresh-token, select-school) : on ne
            // rejoue jamais ces routes, sinon boucle refresh -> 401 -> refresh...
            if (reponse.StatusCode != HttpStatusCode.Unauthorized || EstRouteToken(request))
            {
                return reponse;
            }

            var nouveauJwt = await RafraichirJwtAsync(
                jwtUtiliseParLaRequete: request.Headers.Authorization?.Parameter, ct);

            if (string.IsNullOrEmpty(nouveauJwt))
            {
                return reponse; // refresh impossible : on rend le 401 d'origine
            }

            var rejeu = await CloneAsync(request, ct);
            rejeu.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nouveauJwt);
            reponse.Dispose();
            return await base.SendAsync(rejeu, ct);
        }

        // Refresh serialise : si un autre appel a deja rafraichi pendant notre attente
        // (token stocke different de celui de la requete), on reutilise le sien.
        private async Task<string> RafraichirJwtAsync(string jwtUtiliseParLaRequete, CancellationToken ct)
        {
            await _verrouRefresh.WaitAsync(ct);
            try
            {
                var courant = await _storageService.GetItemAsync<string>(StorageConstants.AuthToken, ct);
                if (string.IsNullOrEmpty(courant))
                {
                    return null; // plus de session : rien a rafraichir
                }

                if (courant != jwtUtiliseParLaRequete)
                {
                    return courant; // deja rafraichi par un appel concurrent
                }

                var tokenService = _services.GetRequiredService<ITokenService>();
                return await tokenService.RefreshTokenAsync();
            }
            catch
            {
                return null; // refresh token expire/invalide -> 401 d'origine
            }
            finally
            {
                _verrouRefresh.Release();
            }
        }

        private static bool EstRouteToken(HttpRequestMessage request)
            => request.RequestUri?.AbsolutePath.Contains("/token/", StringComparison.OrdinalIgnoreCase) == true;

        // Une HttpRequestMessage ne peut pas etre renvoyee deux fois : on la clone
        // (methode, URI, en-tetes, contenu re-bufferise) pour le rejeu.
        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            if (request.Content is not null)
            {
                var octets = await request.Content.ReadAsByteArrayAsync(ct);
                var contenu = new ByteArrayContent(octets);
                foreach (var entete in request.Content.Headers)
                {
                    contenu.Headers.TryAddWithoutValidation(entete.Key, entete.Value);
                }
                clone.Content = contenu;
            }

            foreach (var entete in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(entete.Key, entete.Value);
            }

            return clone;
        }
    }
}
