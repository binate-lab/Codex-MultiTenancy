using App.Infrastructure.Services.Economat;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Economat
{
    // Client du module Economat de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler : le backend en
    // tire tenant + CodeEts, on n'envoie donc jamais l'ecole dans les payloads.
    public class FraisGeneralService : IFraisGeneralService
    {
        private readonly HttpClient _httpClient;

        public FraisGeneralService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<FraisGeneralItem>> GetPostesAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<FraisGeneralItem>>("economat/frais-generaux");
                return data ?? new List<FraisGeneralItem>();
            }
            catch
            {
                return new List<FraisGeneralItem>();
            }
        }

        public Task<FraisGeneralOpResult> CreateAsync(string libelle, decimal montant, int? ordre, bool ok)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("economat/frais-generaux",
                new { libelle, montant, ordre, ok }));

        public Task<FraisGeneralOpResult> UpdateAsync(FraisGeneralItem poste)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"economat/frais-generaux/{poste.Id}", new
            {
                libelle = poste.Libelle,
                montant = poste.Montant,
                ordre = poste.Ordre,
                ok = poste.OK,
            }));

        public Task<FraisGeneralOpResult> DeleteAsync(int id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"economat/frais-generaux/{id}"));

        public async Task<AppliquerFgResult> AppliquerAuxExistantsAsync()
        {
            try
            {
                var reponse = await _httpClient.PostAsync("economat/frais-generaux/appliquer-existants", null);
                if (!reponse.IsSuccessStatusCode) return null;
                return await reponse.Content.ReadFromJsonAsync<AppliquerFgResult>();
            }
            catch
            {
                return null;
            }
        }

        // Envoi + extraction du message metier des reponses non-2xx (409 { error }, 400
        // texte brut...) pour la snackbar — meme approche que NatureVersementService.
        private static async Task<FraisGeneralOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (reponse.IsSuccessStatusCode)
                    return new FraisGeneralOpResult(true);

                return new FraisGeneralOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new FraisGeneralOpResult(false, $"Service indisponible : {ex.Message}");
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
            catch
            {
                // corps non JSON : on retombe sur le texte brut ci-dessous
            }

            var texte = await reponse.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"');
        }

        private sealed class ErreurReponse
        {
            public string Error { get; set; }
        }
    }
}
