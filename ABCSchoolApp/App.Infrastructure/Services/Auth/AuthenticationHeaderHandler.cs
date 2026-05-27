using App.Infrastructure.Constants;
using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace App.Infrastructure.Services.Auth
{
    public class AuthenticationHeaderHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _storageService;

        public AuthenticationHeaderHandler(ILocalStorageService storageService)
        {
            _storageService = storageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            try
            {
                if (request.Headers.Authorization?.Scheme != "Bearer")
                {
                    var savedToken = await _storageService.GetItemAsync<string>(StorageConstants.AuthToken, ct);
                    if (!string.IsNullOrEmpty(savedToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
                    }
                }

                return await base.SendAsync(request, ct);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
