using App.Infrastructure.Services.Eleves;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Eleves
{
    // Client du microservice Scolarite.Api (base = ApiSettings.ScolariteApiUrl) pour le
    // compteur de N° Inscription par ecole (#5).
    public class InscriptionService : IInscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public InscriptionService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        // NumOrdre a repris le role de MatriculeInterne ; l'URL backend historique
        // (eleves/next-matricule-interne) est conservee, elle calcule max(NumOrdre)+1.
        public async Task<int?> GetNextNumOrdreAsync(string codeEts)
        {
            try
            {
                var url = $"{_apiSettings.EleveEndpoints.NextMatriculeInterne}?codeEts={Uri.EscapeDataString(codeEts)}";
                var resp = await _httpClient.GetFromJsonAsync<NextMatriculeResponse>(url);
                return resp?.NextMatricule;
            }
            catch
            {
                // Indisponible (service down, non autorise…) : on laisse le champ libre (saisie manuelle).
                return null;
            }
        }

        private record NextMatriculeResponse(int NextMatricule);
    }
}
