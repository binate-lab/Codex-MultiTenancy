using App.Infrastructure.Services.Fne;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Fne
{
    // Client du module FNE de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT école-scoped est propagé par l'AuthenticationHeaderHandler : le backend en tire
    // tenant + CodeEts, on n'envoie donc jamais l'école dans les payloads.
    public class FneService : IFneService
    {
        private readonly HttpClient _httpClient;

        public FneService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<ParametreFneDto?> GetParametresAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ParametreFneDto?>("economat/fne/parametres");
            }
            catch
            {
                return null;
            }
        }

        public Task<FneOpResult> SaveParametresAsync(ParametreFneDto parametres)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync("economat/fne/parametres", parametres));

        public async Task<IReadOnlyList<FactureFneItem>> GetFacturesAsync(string? statut)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(statut)
                    ? "economat/fne/factures"
                    : $"economat/fne/factures?statut={Uri.EscapeDataString(statut)}";
                var data = await _httpClient.GetFromJsonAsync<List<FactureFneItem>>(url);
                return data ?? new List<FactureFneItem>();
            }
            catch
            {
                return new List<FactureFneItem>();
            }
        }

        public Task<FneOpResult> RelancerAsync(Guid factureId)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync($"economat/fne/factures/{factureId}/relancer", new { }));

        private static async Task<FneOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                return reponse.IsSuccessStatusCode
                    ? new FneOpResult(true)
                    : new FneOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new FneOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        private static async Task<string> LireErreurAsync(HttpResponseMessage reponse)
        {
            try
            {
                var contenu = await reponse.Content.ReadFromJsonAsync<ErreurReponse>();
                if (!string.IsNullOrWhiteSpace(contenu?.Error))
                    return contenu.Error;
            }
            catch { /* corps non JSON : repli sur le texte brut */ }

            var texte = await reponse.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"');
        }

        private sealed class ErreurReponse { public string? Error { get; set; } }
    }
}
