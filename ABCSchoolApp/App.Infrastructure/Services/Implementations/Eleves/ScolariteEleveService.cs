using App.Infrastructure.Services.Eleves;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Eleves
{
    // Client du microservice Scolarite.Api (base = ApiSettings.ScolariteApiUrl) pour la
    // liste des eleves (ScolariteDb). Le JWT ecole-scoped est propage par l'AuthHeaderHandler.
    public class ScolariteEleveService : IScolariteEleveService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public ScolariteEleveService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<IReadOnlyList<EleveScolariteItem>> GetElevesAsync(string codeEts, string annee = null)
        {
            try
            {
                var url = $"{_apiSettings.EleveEndpoints.Liste}?codeEts={Uri.EscapeDataString(codeEts)}";
                if (!string.IsNullOrWhiteSpace(annee))
                    url += $"&annee={Uri.EscapeDataString(annee)}";

                var data = await _httpClient.GetFromJsonAsync<List<EleveScolariteItem>>(url);
                return data ?? new List<EleveScolariteItem>();
            }
            catch
            {
                // Indisponible (service down, non autorise…) : grille vide plutot qu'une erreur.
                return new List<EleveScolariteItem>();
            }
        }

        public async Task<bool> MajTelephoneCorrespondantAsync(Guid eleveId, string telephone)
        {
            try
            {
                var reponse = await _httpClient.PutAsJsonAsync(
                    $"eleves/{eleveId}/telephone-correspondant",
                    new { telephone });
                return reponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MajCodeParentAsync(Guid eleveId, string codeParent)
        {
            try
            {
                var reponse = await _httpClient.PutAsJsonAsync(
                    $"eleves/{eleveId}/code-parent",
                    new { codeParent });
                return reponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IReadOnlyList<VersementsJourNiveauItem>> GetVersementsPeriodeAsync(
            string codeEts, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            try
            {
                var url = $"versements/journalier?codeEts={Uri.EscapeDataString(codeEts)}";
                if (dateDebut is not null)
                    url += $"&dateDebut={Uri.EscapeDataString(dateDebut.Value.ToString("yyyy-MM-dd"))}";
                if (dateFin is not null)
                    url += $"&dateFin={Uri.EscapeDataString(dateFin.Value.ToString("yyyy-MM-dd"))}";

                var data = await _httpClient.GetFromJsonAsync<List<VersementsJourNiveauItem>>(url);
                return data ?? new List<VersementsJourNiveauItem>();
            }
            catch
            {
                return new List<VersementsJourNiveauItem>();
            }
        }
    }
}
