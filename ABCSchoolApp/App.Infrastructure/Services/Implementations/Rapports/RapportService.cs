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

        // Sans periode : l'API ignore les dates (contrat RapportPeriodeRequest inchange,
        // on envoie la date du jour pour les deux bornes).
        public Task<byte[]> GetRapportRecouvrementPdfAsync(
            string ecole, string logoBase64, string ville, string anneeScolaire)
        {
            var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
            return PostRapportAsync("rapports/versements/recouvrement", aujourdhui, aujourdhui, ecole, logoBase64, ville, anneeScolaire);
        }

        // Sans periode (point au jour). `classe` vide/null = toutes les classes ; sinon une
        // seule classe. Corps dedie (avec « classe ») distinct du RapportPeriodeRequest.
        public async Task<byte[]> GetBilanEleveClassePdfAsync(
            string classe, string ecole, string logoBase64, string ville, string anneeScolaire)
        {
            const string route = "rapports/versements/bilan-eleve-classe";

            HttpResponseMessage reponse;
            try
            {
                reponse = await _httpClient.PostAsJsonAsync(route, new
                {
                    classe,
                    ecole,
                    ville,
                    anneeScolaire,
                    logoBase64
                });
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Appel « {route} » impossible : {ex.Message}", ex);
            }

            if (!reponse.IsSuccessStatusCode)
            {
                var corps = await reponse.Content.ReadAsStringAsync();
                if (corps.Length > 300) corps = corps[..300];
                throw new HttpRequestException(
                    $"HTTP {(int)reponse.StatusCode} {reponse.ReasonPhrase} sur « {route} ». {corps}".Trim());
            }

            return await reponse.Content.ReadAsByteArrayAsync();
        }

        private async Task<byte[]> PostRapportAsync(
            string route, DateOnly debut, DateOnly fin, string ecole, string logoBase64, string ville, string anneeScolaire)
        {
            HttpResponseMessage reponse;
            try
            {
                reponse = await _httpClient.PostAsJsonAsync(route, new
                {
                    dateDebut = debut,
                    dateFin = fin,
                    ecole,
                    ville,
                    anneeScolaire,
                    logoBase64
                });
            }
            catch (Exception ex)
            {
                // La requete n'a meme pas abouti cote reseau : contenu mixte (page https ->
                // API http), CORS bloque, DNS/TLS... On remonte la vraie cause au lieu de null.
                throw new HttpRequestException($"Appel « {route} » impossible : {ex.Message}", ex);
            }

            if (!reponse.IsSuccessStatusCode)
            {
                var corps = await reponse.Content.ReadAsStringAsync();
                if (corps.Length > 300) corps = corps[..300];
                throw new HttpRequestException(
                    $"HTTP {(int)reponse.StatusCode} {reponse.ReasonPhrase} sur « {route} ». {corps}".Trim());
            }

            return await reponse.Content.ReadAsByteArrayAsync();
        }
    }
}
