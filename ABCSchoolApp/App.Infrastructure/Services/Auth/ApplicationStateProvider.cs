using TrajanEcole.Shared.Library.Constants;
using App.Infrastructure.Constants;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace App.Infrastructure.Services.Auth
{
    public class ApplicationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly HttpClient _httpClient;
        public ClaimsPrincipal AuthenticationStateUser { get; set; }

        public ApplicationStateProvider(ILocalStorageService localStorageService, HttpClient httpClient)
        {
            _localStorageService = localStorageService;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var savedToken = await _localStorageService.GetItemAsync<string>(StorageConstants.AuthToken);

            if (string.IsNullOrEmpty(savedToken))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = GetClaimsFromJwt(savedToken).ToList();

            // Token expiré (claim "exp", secondes Unix) : on le purge et on repasse en
            // anonyme pour forcer le passage par la page d'identification.
            if (EstTokenExpire(claims))
            {
                await _localStorageService.RemoveItemAsync(StorageConstants.AuthToken);
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);

            var state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
            AuthenticationStateUser = state.User;
            return state;
        }

        public void MarkUserAuthenticated()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void MarkUserAsLoggedOut()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

            var authState = Task.FromResult(new AuthenticationState(anonymousUser));

            NotifyAuthenticationStateChanged(authState);
        }

        public async Task<ClaimsPrincipal> GetAuthenticationStateProviderUserAsync()
        {
            var state = await GetAuthenticationStateAsync();
            return state.User;
        }

        #region Helpers
        private static bool EstTokenExpire(IEnumerable<Claim> claims)
        {
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            // Pas de claim exp lisible : par sécurité, on considère le token comme invalide.
            if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expSeconds))
                return true;

            var expiration = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            return expiration <= DateTimeOffset.UtcNow;
        }

        private IEnumerable<Claim> GetClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);

            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                keyValuePairs.TryGetValue(ClaimTypes.Role, out var roles);

                if (roles != null)
                {
                    if (roles.ToString().Trim().StartsWith('['))
                    {
                        var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());
                        claims.AddRange(parsedRoles.Select(roleName => new Claim(ClaimTypes.Role, roleName)));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                    }

                    keyValuePairs.Remove(ClaimTypes.Role);
                }

                keyValuePairs.TryGetValue(ClaimConstants.Permission, out var permissions);

                if (permissions != null)
                {
                    if (permissions.ToString().Trim().StartsWith('['))
                    {
                        var parsedPermissions = JsonSerializer.Deserialize<string[]>(permissions.ToString());
                        claims.AddRange(parsedPermissions.Select(permission => new Claim(ClaimConstants.Permission, permission)));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimConstants.Permission, permissions.ToString()));
                    }
                    keyValuePairs.Remove(ClaimConstants.Permission);
                }

                claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));
            }
            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64Payload)
        {
            switch (base64Payload.Length % 4)
            {
                case 2: base64Payload += "=="; break;
                case 3: base64Payload += "="; break;
            }

            return Convert.FromBase64String(base64Payload);
        }
        #endregion
    }
}
