using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using App.Infrastructure.Services.Rapports;

namespace App.Infrastructure.Services.Implementations.Rapports
{
    // Client des rapports mensuels (base = ApiSettings:ScolariteApiUrl). Le JWT ecole-scoped
    // est propage par l'AuthenticationHeaderHandler.
    public class RapportService : IRapportService
    {
        private readonly HttpClient _httpClient;

        public RapportService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<byte[]> GetRapportMensuelPdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire)
            => PostRapportAsync("rapports/versements/mensuel", debut, fin, ecole, logoBase64, ville, anneeScolaire);

        public Task<byte[]> GetRapportParClassePdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire)
            => PostRapportAsync("rapports/versements/par-classe", debut, fin, ecole, logoBase64, ville, anneeScolaire);

        public Task<byte[]> GetRapportParNaturePdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire)
            => PostRapportAsync("rapports/versements/par-nature", debut, fin, ecole, logoBase64, ville, anneeScolaire);

        public Task<byte[]> GetRapportParModePaiementPdfAsync(
            DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire)
            => PostRapportAsync("rapports/versements/par-mode-paiement", debut, fin, ecole, logoBase64, ville, anneeScolaire);

        private async Task<byte[]> PostRapportAsync(
            string route, DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire)
        {
            try
            {
                var reponse = await _httpClient.PostAsJsonAsync(route, new
                {
                    dateDebut = debut,
                    dateFin = fin,
                    ecole,
                    ville,
                    anneeScolaire,
                    logoBase64
                });

                if (!reponse.IsSuccessStatusCode) return null;

                return await reponse.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}
