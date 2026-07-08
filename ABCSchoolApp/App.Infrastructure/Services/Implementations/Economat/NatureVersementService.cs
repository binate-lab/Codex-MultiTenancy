using App.Infrastructure.Services.Economat;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Economat
{
    // Client du module Economat de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler : le backend en
    // tire tenant + CodeEts, on n'envoie donc jamais l'ecole dans les payloads.
    public class NatureVersementService : INatureVersementService
    {
        private readonly HttpClient _httpClient;

        public NatureVersementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<NatureVersementItem>> GetNaturesAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<NatureVersementItem>>("economat/natures");
                return data ?? new List<NatureVersementItem>();
            }
            catch
            {
                return new List<NatureVersementItem>();
            }
        }

        public Task<NatureOpResult> CreateAsync(string libelle, int? ordre, bool ok, bool estInscription)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("economat/natures",
                new { libelle, ordre, ok, estInscription }));

        public Task<NatureOpResult> UpdateAsync(NatureVersementItem nature)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"economat/natures/{nature.Id}", new
            {
                libelle = nature.Libelle,
                ordre = nature.Ordre,
                ok = nature.OK,
                estInscription = nature.EstInscription,
            }));

        public Task<NatureOpResult> DeleteAsync(int id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"economat/natures/{id}"));

        // Envoi + extraction du message metier des reponses non-2xx (409 { error }, 400
        // texte brut...) pour la snackbar — meme approche que EcheancierService.
        private static async Task<NatureOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (reponse.IsSuccessStatusCode)
                    return new NatureOpResult(true);

                return new NatureOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new NatureOpResult(false, $"Service indisponible : {ex.Message}");
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
