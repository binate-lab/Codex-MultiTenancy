using App.Infrastructure.Services.Orange;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Orange
{
    // Client du CRUD des comptes marchands Orange (base = ApiSettings:ScolariteApiUrl).
    // Le JWT école-scoped est propagé par l'AuthenticationHeaderHandler : le backend en tire
    // tenant + CodeEts, on n'envoie donc jamais l'école dans les payloads.
    public class CompteMarchandOrangeService : ICompteMarchandOrangeService
    {
        private readonly HttpClient _httpClient;

        public CompteMarchandOrangeService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<IReadOnlyList<CompteMarchandItem>> GetAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<CompteMarchandItem>>("economat/marchands-orange");
                return data ?? new List<CompteMarchandItem>();
            }
            catch
            {
                return new List<CompteMarchandItem>();
            }
        }

        // Création : renvoie le secret généré (visible une seule fois).
        public Task<MarchandOpResult> CreateAsync(string codeMarchand, string? libelle)
            => EnvoyerAvecSecretAsync(() => _httpClient.PostAsJsonAsync("economat/marchands-orange",
                new { codeMarchand, libelle }));

        public Task<MarchandOpResult> UpdateAsync(int id, string codeMarchand, string? libelle, bool actif)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"economat/marchands-orange/{id}",
                new { codeMarchand, libelle, actif }));

        // Rotation : renvoie le nouveau secret (visible une seule fois).
        public Task<MarchandOpResult> RotationSecretAsync(int id)
            => EnvoyerAvecSecretAsync(() => _httpClient.PostAsJsonAsync(
                $"economat/marchands-orange/{id}/rotation-secret", new { }));

        public Task<MarchandOpResult> DeleteAsync(int id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"economat/marchands-orange/{id}"));

        // Écriture simple (pas de secret dans la réponse).
        private static async Task<MarchandOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                return reponse.IsSuccessStatusCode
                    ? new MarchandOpResult(true)
                    : new MarchandOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new MarchandOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        // Écriture qui renvoie un secret en clair (création / rotation) : on l'extrait du corps.
        private static async Task<MarchandOpResult> EnvoyerAvecSecretAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (!reponse.IsSuccessStatusCode)
                    return new MarchandOpResult(false, await LireErreurAsync(reponse));

                var corps = await reponse.Content.ReadFromJsonAsync<SecretReponse>();
                return new MarchandOpResult(true, Secret: corps?.Secret);
            }
            catch (Exception ex)
            {
                return new MarchandOpResult(false, $"Service indisponible : {ex.Message}");
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

        private sealed class SecretReponse { public string? Secret { get; set; } }
        private sealed class ErreurReponse { public string? Error { get; set; } }
    }
}
