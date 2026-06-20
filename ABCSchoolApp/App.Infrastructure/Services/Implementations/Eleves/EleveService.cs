using App.Infrastructure.Services.Eleves;
using System.Net.Http.Json;
using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Services.Implementations.Eleves
{
    // Client du microservice Eleves.Api (base address dediee = ApiSettings.ElevesApiUrl).
    public class EleveService : IEleveService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public EleveService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<EleveCreationResult> CreateAsync(CreateEleveRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_apiSettings.EleveEndpoints.Create, request);

                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<CreateEleveResponse>();
                    return new EleveCreationResult(true, created?.Id ?? Guid.Empty, null);
                }

                var error = await response.Content.ReadAsStringAsync();
                return new EleveCreationResult(false, Guid.Empty,
                    string.IsNullOrWhiteSpace(error) ? $"Echec ({(int)response.StatusCode})" : error);
            }
            catch (Exception ex)
            {
                return new EleveCreationResult(false, Guid.Empty, ex.Message);
            }
        }

        private record CreateEleveResponse(Guid Id);
    }
}
