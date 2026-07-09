using App.Infrastructure.Services.Eleves;
using System.Net.Http.Json;
using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Services.Implementations.Eleves
{
    // Client du referentiel Eleves (module de Pedagogie.Api ; base = ApiSettings.ElevesApiUrl, port 5103).
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
                    return new EleveCreationResult(true, created?.Id ?? Guid.Empty, null, created?.NumOrdre ?? 0);
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

        public async Task<bool> MatriculeExisteAsync(string codeEts, string matricule)
        {
            try
            {
                var url = $"{_apiSettings.EleveEndpoints.MatriculeExiste}" +
                          $"?codeEts={Uri.EscapeDataString(codeEts)}&matricule={Uri.EscapeDataString(matricule)}";
                var resp = await _httpClient.GetFromJsonAsync<MatriculeExisteResponse>(url);
                return resp?.Existe ?? false;
            }
            catch
            {
                // Indisponible : on ne bloque pas la saisie (la garde definitive est a la creation).
                return false;
            }
        }

        public async Task<IReadOnlyList<ElevePedagogieItem>> GetElevesAsync(string codeEts)
        {
            try
            {
                // GET /eleves?codeEts=... (Pedagogie.Api / ListesDeClasse). Reponse enveloppee
                // { "eleves": [ ... ] }. Le JWT ecole-scoped est propage par l'AuthHeaderHandler.
                var url = $"eleves?codeEts={Uri.EscapeDataString(codeEts)}";
                var data = await _httpClient.GetFromJsonAsync<PedagogieElevesResponse>(url);
                return data?.Eleves ?? new List<ElevePedagogieItem>();
            }
            catch
            {
                // Indisponible (service down, non autorise…) : grille vide plutot qu'une erreur.
                return new List<ElevePedagogieItem>();
            }
        }

        public async Task<bool> MajPhotoAsync(Guid eleveId, string imageFile)
        {
            try
            {
                // PUT /eleves/{id}/photo { imageFile } sur Pedagogie.Api. Le JWT ecole-scoped
                // est propage par l'AuthHeaderHandler.
                var resp = await _httpClient.PutAsJsonAsync($"eleves/{eleveId}/photo", new { imageFile });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private record CreateEleveResponse(Guid Id, int NumOrdre);
        private record MatriculeExisteResponse(bool Existe);
        private record PedagogieElevesResponse(List<ElevePedagogieItem> Eleves);
    }
}
