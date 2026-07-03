using App.Infrastructure.Services.Economat;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Economat
{
    // Client du module Economat de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler : le backend en
    // tire tenant + CodeEts, on n'envoie donc jamais l'ecole dans les payloads.
    public class EcheancierService : IEcheancierService
    {
        private readonly HttpClient _httpClient;

        public EcheancierService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<ModaliteVersementItem>> GetModalitesAsync(string annee = null)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(annee)
                    ? "economat/modalites"
                    : $"economat/modalites?annee={Uri.EscapeDataString(annee)}";
                var data = await _httpClient.GetFromJsonAsync<List<ModaliteVersementItem>>(url);
                return data ?? new List<ModaliteVersementItem>();
            }
            catch
            {
                return new List<ModaliteVersementItem>();
            }
        }

        public Task<EcheancierOpResult> CreateModaliteAsync(string anneeScolaire, string niveauCode, string statut)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("economat/modalites",
                new { anneeScolaire, niveauCode, statut }));

        public Task<EcheancierOpResult> UpdateMontantsAsync(ModaliteVersementItem ligne)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"economat/modalites/{ligne.Id}", new
            {
                inscription = ligne.Inscription,
                septembre = ligne.Septembre,
                octobre = ligne.Octobre,
                novembre = ligne.Novembre,
                decembre = ligne.Decembre,
                janvier = ligne.Janvier,
                fevrier = ligne.Fevrier,
                mars = ligne.Mars,
                avril = ligne.Avril,
                mai = ligne.Mai,
            }));

        public Task<EcheancierOpResult> DeleteModaliteAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"economat/modalites/{id}"));

        // Envoi + extraction du message metier des reponses non-2xx (meme approche que
        // StructureService : 409 { error }, 400 texte brut...) pour la snackbar.
        private static async Task<EcheancierOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (reponse.IsSuccessStatusCode)
                {
                    return new EcheancierOpResult(true);
                }

                var erreur = await LireErreurAsync(reponse);
                return new EcheancierOpResult(false, erreur);
            }
            catch (Exception ex)
            {
                return new EcheancierOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        private static async Task<string> LireErreurAsync(HttpResponseMessage reponse)
        {
            try
            {
                var contenu = await reponse.Content.ReadFromJsonAsync<ErreurReponse>();
                if (!string.IsNullOrWhiteSpace(contenu?.Error))
                {
                    return contenu.Error;
                }
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
