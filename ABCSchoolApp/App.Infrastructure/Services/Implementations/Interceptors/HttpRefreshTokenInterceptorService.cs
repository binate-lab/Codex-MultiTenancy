using App.Infrastructure.Services.Identity;
using App.Infrastructure.Services.Interceptors;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Headers;
using Toolbelt.Blazor;

namespace App.Infrastructure.Services.Implementations.Interceptors
{
    public class HttpRefreshTokenInterceptorService : IHttpRefreshTokenInterceptorService
    {
        private readonly HttpClientInterceptor _httpClientInterceptor;
        private readonly ITokenService _tokenService;
        private readonly NavigationManager _navigation;
        private readonly ISnackbar _snackbar;

        public HttpRefreshTokenInterceptorService(
            HttpClientInterceptor httpClientInterceptor,
            ITokenService tokenService,
            NavigationManager navigation,
            ISnackbar snackbar)
        {
            _httpClientInterceptor = httpClientInterceptor;
            _tokenService = tokenService;
            _navigation = navigation;
            _snackbar = snackbar;
        }

        public async Task InterceptBeforeHttpRequestAsync(object sender, HttpClientInterceptorEventArgs eventArgs)
        {
            var absPath = eventArgs.Request.RequestUri.AbsolutePath;

            if (!absPath.Contains("token"))
            {
                try
                {
                    var newlyRefreshedJwt = await _tokenService.TryForceRefreshTokenAsync();

                    if (!string.IsNullOrEmpty(newlyRefreshedJwt))
                    {
                        eventArgs.Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newlyRefreshedJwt);
                    }
                }
                catch (Exception)
                {
                    _snackbar.Add("Vous êtes déconnecté", Severity.Error);
                    await _tokenService.LogoutAsync();
                    _navigation.NavigateTo("/");
                }
            }
        }

        public void RegisterEvent() => _httpClientInterceptor.BeforeSendAsync += InterceptBeforeHttpRequestAsync;
    }
}
