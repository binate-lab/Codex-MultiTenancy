using App.Infrastructure.Services.Eleves;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Eleves
{
    // Client du module Versements de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler.
    public class VersementService : IVersementService
    {
        private readonly HttpClient _httpClient;

        public VersementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<VersementsEleveReponse> GetVersementsAsync(Guid eleveId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<VersementsEleveReponse>($"eleves/{eleveId}/versements");
            }
            catch
            {
                return null;
            }
        }

        public async Task<VersementOpResult> CreateAsync(Guid eleveId, decimal montant, DateTime? date,
            string nature, string moyenPaiement, string referenceOperation)
        {
            try
            {
                var reponse = await _httpClient.PostAsJsonAsync($"eleves/{eleveId}/versements", new
                {
                    montant,
                    dateVersement = date,
                    nature,
                    moyenPaiement,
                    referenceOperation,
                });

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }
    }
}
