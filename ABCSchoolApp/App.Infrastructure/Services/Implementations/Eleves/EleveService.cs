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

        public async Task<bool> MajStatutAsync(Guid eleveId, string statut)
        {
            try
            {
                var resp = await _httpClient.PutAsJsonAsync($"eleves/{eleveId}/statut", new { statut });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MajClasseAsync(Guid eleveId, string classe)
        {
            try
            {
                var resp = await _httpClient.PutAsJsonAsync($"eleves/{eleveId}/classe", new { classe });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MajCycleNiveauAsync(Guid eleveId, int cycle, string niveau)
        {
            try
            {
                var resp = await _httpClient.PutAsJsonAsync(
                    $"eleves/{eleveId}/cycle-niveau", new { cycle, niveau });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MajTuteurAsync(Guid eleveId, string nom, string prenom, string telephone1, string telephone2)
        {
            try
            {
                var resp = await _httpClient.PutAsJsonAsync(
                    $"eleves/{eleveId}/tuteur",
                    new { nom, prenom, telephone1, telephone2 });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MajLv2Async(Guid eleveId, string lv2)
        {
            try
            {
                var resp = await _httpClient.PutAsJsonAsync($"eleves/{eleveId}/lv2", new { lv2 });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MajArtsAsync(Guid eleveId, string arts)
        {
            try
            {
                var resp = await _httpClient.PutAsJsonAsync($"eleves/{eleveId}/arts", new { arts });
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<RegenererMatriculesResult> RegenererMatriculesAsync(bool complet)
        {
            try
            {
                // POST /eleves/matricules/regenerer?complet=... (sans corps). Le JWT ecole-scoped
                // est propage par l'AuthHeaderHandler ; l'ecole est deduite des claims cote serveur.
                var resp = await _httpClient.PostAsync(
                    $"eleves/matricules/regenerer?complet={(complet ? "true" : "false")}", null);

                if (!resp.IsSuccessStatusCode)
                {
                    var error = await resp.Content.ReadAsStringAsync();
                    return new RegenererMatriculesResult(false, 0, 0, 0,
                        string.IsNullOrWhiteSpace(error) ? $"Echec ({(int)resp.StatusCode})" : error);
                }

                var data = await resp.Content.ReadFromJsonAsync<RegenererResponse>();
                return new RegenererMatriculesResult(true,
                    data?.Total ?? 0, data?.Corriges ?? 0, data?.Regenerations ?? 0);
            }
            catch (Exception ex)
            {
                return new RegenererMatriculesResult(false, 0, 0, 0, ex.Message);
            }
        }

        private record CreateEleveResponse(Guid Id, int NumOrdre);
        private record MatriculeExisteResponse(bool Existe);
        private record PedagogieElevesResponse(List<ElevePedagogieItem> Eleves);
        private record RegenererResponse(int Total, int Corriges, int Regenerations);
    }
}
