using TrajanEcole.Shared.Library.Models.Requests.Token;
using TrajanEcole.Shared.Library.Models.Responses.Token;
using TrajanEcole.Shared.Library.Wrappers;
using App.Infrastructure.Constants;
using App.Infrastructure.Extensions;
using App.Infrastructure.Services.Auth;
using App.Infrastructure.Services.Identity;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Identity
{
    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorageService;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ApiSettings _apiSettings;

        public TokenService(HttpClient httpClient, 
            ILocalStorageService localStorageService, 
            AuthenticationStateProvider authStateProvider, 
            ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _localStorageService = localStorageService;
            _authStateProvider = authStateProvider;
            _apiSettings = apiSettings;
        }

        public async Task<IResponseWrapper> LoginAsync(string tenant, TokenRequest request)
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                _apiSettings.TokenEndpoints.Login)
            {
                Content = JsonContent.Create(request)
            };

            AddTenantHeader(httpRequest, headerName: "tenant", value: tenant);

            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            var result = await response.WrapToResponse<TokenResponse>();

            if (result.IsSuccessful)
            {
                var token = result.Data.Jwt;
                var refreshToken = result.Data.RefreshToken;

                await _localStorageService.SetItemAsync(StorageConstants.AuthToken, token);
                await _localStorageService.SetItemAsync(StorageConstants.RefreshToken, refreshToken);

                ((ApplicationStateProvider)_authStateProvider).MarkUserAuthenticated();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await ResponseWrapper.SuccessAsync();
            }
            else
            {
                return await ResponseWrapper.FailAsync(messages: result.Messages);
            }
        }
        public async Task<IResponseWrapper> LogoutAsync()
        {
            await _localStorageService.RemoveItemAsync(StorageConstants.AuthToken);
            await _localStorageService.RemoveItemAsync(StorageConstants.RefreshToken);

            ((ApplicationStateProvider)_authStateProvider).MarkUserAsLoggedOut();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            return await ResponseWrapper.SuccessAsync(message: "Déconnection éffectuée avec succès");
        }

        public async Task<string> RefreshTokenAsync()
        {
            var currentJwt = await _localStorageService.GetItemAsync<string>(StorageConstants.AuthToken);
            var currentRefreshToken = await _localStorageService.GetItemAsync<string>(StorageConstants.RefreshToken);

            var response = await _httpClient.PostAsJsonAsync(_apiSettings.TokenEndpoints.RefreshToken, new RefreshTokenRequest
            {
                CurrentJwt = currentJwt,
                CurrentRefreshToken = currentRefreshToken
            });

            var result = await response.WrapToResponse<TokenResponse>();

            if (result.IsSuccessful)
            {
                currentJwt = result.Data.Jwt;
                currentRefreshToken = result.Data.RefreshToken;

                await _localStorageService.SetItemAsync(StorageConstants.AuthToken, currentJwt);
                await _localStorageService.SetItemAsync(StorageConstants.RefreshToken, currentRefreshToken);

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", currentJwt);
                return currentJwt;
            }
            else
            {
                throw new ApplicationException("Somthing went wrong during refresh token generation.");
            }

        }

        public async Task<string> TryForceRefreshTokenAsync()
        {
            var currentRefreshToken = await _localStorageService.GetItemAsync<string>(StorageConstants.RefreshToken);
            if (string.IsNullOrEmpty(currentRefreshToken)) return string.Empty;

            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            var user = authState.User;
            var exp = user.FindFirst(c => c.Type.Equals("exp"))?.Value;
            var expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));
            var currentTime = DateTime.UtcNow;

            var diff = expTime - currentTime;

            // Only within last five minutes to expiry
            if (diff.TotalMinutes <= 5)
            {
                if (diff.TotalMinutes < 0)
                {
                    await LogoutAsync();
                }
                else
                {
                    return await RefreshTokenAsync();
                }
            }
            return string.Empty;
        }

        #region Helpers
        private static void AddTenantHeader(HttpRequestMessage request, string headerName, string value)
        {
            if (string.IsNullOrEmpty(value) || request.Headers.Contains(headerName))
                return;

            request.Headers.TryAddWithoutValidation(headerName, value);
        }       

        #endregion
    }
}
