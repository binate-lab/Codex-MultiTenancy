using App.Infrastructure.Services.Orange;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Orange
{
    // Client de supervision des paiements Orange (base = ApiSettings:ScolariteApiUrl).
    // JWT école-scoped propagé par l'AuthenticationHeaderHandler → tenant + CodeEts côté backend.
    public class PaiementOrangeService : IPaiementOrangeService
    {
        private readonly HttpClient _httpClient;

        public PaiementOrangeService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<IReadOnlyList<PaiementOrangeItem>> GetAsync(string? statut = null)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(statut)
                    ? "orange/paiements"
                    : $"orange/paiements?statut={Uri.EscapeDataString(statut)}";
                var data = await _httpClient.GetFromJsonAsync<List<PaiementOrangeItem>>(url);
                return data ?? new List<PaiementOrangeItem>();
            }
            catch
            {
                return new List<PaiementOrangeItem>();
            }
        }

        public Task<PaiementOpResult> ValiderAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync($"orange/paiements/{id}/valider", new { }));

        public Task<PaiementOpResult> ValiderManuAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync($"orange/paiements/{id}/valider-manuel", new { }));

        public Task<PaiementOpResult> RattacherAsync(Guid id, string matricule)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync(
                $"orange/paiements/{id}/rattacher", new { matricule }));

        public Task<PaiementOpResult> RejeterAsync(Guid id, string? note)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync(
                $"orange/paiements/{id}/rejeter", new { note }));

        private static async Task<PaiementOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                return reponse.IsSuccessStatusCode
                    ? new PaiementOpResult(true)
                    : new PaiementOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new PaiementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        private static async Task<string> LireErreurAsync(HttpResponseMessage reponse)
        {
            try
            {
                var contenu = await reponse.Content.ReadFromJsonAsync<ErreurReponse>();
                if (!string.IsNullOrWhiteSpace(contenu?.Message))
                    return contenu.Message;
            }
            catch { /* corps non JSON : repli sur le texte brut */ }

            var texte = await reponse.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"');
        }

        // Les endpoints /orange/paiements renvoient { message } sur erreur (400/404).
        private sealed class ErreurReponse { public string? Message { get; set; } }
    }
}
