using App.Infrastructure.Services.Economat;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Economat
{
    // Client du module Economat de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler : le backend en
    // tire tenant + CodeEts, on n'envoie donc jamais l'ecole dans les payloads.
    public class TypeReductionService : ITypeReductionService
    {
        private readonly HttpClient _httpClient;

        public TypeReductionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<TypeReductionItem>> GetTypesAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<TypeReductionItem>>("economat/types-reduction");
                return data ?? new List<TypeReductionItem>();
            }
            catch
            {
                return new List<TypeReductionItem>();
            }
        }

        public Task<TypeReductionOpResult> CreateAsync(string libelle, int? ordre, bool ok)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("economat/types-reduction",
                new { libelle, ordre, ok }));

        public Task<TypeReductionOpResult> UpdateAsync(TypeReductionItem type)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"economat/types-reduction/{type.Id}", new
            {
                libelle = type.Libelle,
                ordre = type.Ordre,
                ok = type.OK,
            }));

        public Task<TypeReductionOpResult> DeleteAsync(int id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"economat/types-reduction/{id}"));

        // Envoi + extraction du message metier des reponses non-2xx (409 { error }, 400
        // texte brut...) pour la snackbar — meme approche que NatureVersementService.
        private static async Task<TypeReductionOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (reponse.IsSuccessStatusCode)
                    return new TypeReductionOpResult(true);

                return new TypeReductionOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new TypeReductionOpResult(false, $"Service indisponible : {ex.Message}");
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
