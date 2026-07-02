using App.Infrastructure.Services.Structures;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Structures
{
    // Client du module Structures de pedagogie-api (base = ApiSettings:PedagogieApiUrl,
    // port 5103). Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler :
    // le backend en tire tenant + CodeEts, on n'envoie donc jamais l'ecole dans les payloads.
    public class StructureService : IStructureService
    {
        private readonly HttpClient _httpClient;

        public StructureService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // --- Cycles (l'arbre cycles→niveaux se lit d'un bloc) --------------------

        public async Task<IReadOnlyList<CycleItem>> GetCyclesAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<CycleItem>>("structures/cycles");
                return data ?? new List<CycleItem>();
            }
            catch
            {
                return new List<CycleItem>();
            }
        }

        public Task<StructureOpResult> CreateCycleAsync(int numero, string libelle, int ordre)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("structures/cycles", new { numero, libelle, ordre }));

        public Task<StructureOpResult> UpdateCycleAsync(Guid id, int numero, string libelle, int ordre)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"structures/cycles/{id}", new { numero, libelle, ordre }));

        public Task<StructureOpResult> DeleteCycleAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"structures/cycles/{id}"));

        // --- Niveaux --------------------------------------------------------------

        public Task<StructureOpResult> CreateNiveauAsync(Guid cycleId, string code, string libelle, int ordre)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("structures/niveaux", new { cycleId, code, libelle, ordre }));

        public Task<StructureOpResult> UpdateNiveauAsync(Guid id, Guid cycleId, string code, string libelle, int ordre)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"structures/niveaux/{id}", new { cycleId, code, libelle, ordre }));

        public Task<StructureOpResult> DeleteNiveauAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"structures/niveaux/{id}"));

        // --- Classes ----------------------------------------------------------------

        public async Task<IReadOnlyList<ClasseItem>> GetClassesAsync(string annee = null)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(annee)
                    ? "structures/classes"
                    : $"structures/classes?annee={Uri.EscapeDataString(annee)}";
                var data = await _httpClient.GetFromJsonAsync<List<ClasseItem>>(url);
                return data ?? new List<ClasseItem>();
            }
            catch
            {
                return new List<ClasseItem>();
            }
        }

        public Task<StructureOpResult> CreateClasseAsync(Guid niveauId, string anneeScolaire, string libelle)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("structures/classes", new { niveauId, anneeScolaire, libelle }));

        public Task<StructureOpResult> UpdateClasseAsync(Guid id, Guid niveauId, string anneeScolaire, string libelle)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"structures/classes/{id}", new { niveauId, anneeScolaire, libelle }));

        public Task<StructureOpResult> DeleteClasseAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"structures/classes/{id}"));

        // --- Matières ---------------------------------------------------------------

        public async Task<IReadOnlyList<MatiereItem>> GetMatieresAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<MatiereItem>>("structures/matieres");
                return data ?? new List<MatiereItem>();
            }
            catch
            {
                return new List<MatiereItem>();
            }
        }

        public Task<StructureOpResult> CreateMatiereAsync(string code, string libelle, int ordre)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("structures/matieres", new { code, libelle, ordre }));

        public Task<StructureOpResult> UpdateMatiereAsync(Guid id, string code, string libelle, int ordre)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"structures/matieres/{id}", new { code, libelle, ordre }));

        public Task<StructureOpResult> DeleteMatiereAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"structures/matieres/{id}"));

        // Envoi + extraction du message metier des reponses non-2xx (409 { error },
        // 400 texte brut...) pour affichage direct dans la snackbar.
        private static async Task<StructureOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (reponse.IsSuccessStatusCode)
                {
                    return new StructureOpResult(true);
                }

                var erreur = await LireErreurAsync(reponse);
                return new StructureOpResult(false, erreur);
            }
            catch (Exception ex)
            {
                return new StructureOpResult(false, $"Service indisponible : {ex.Message}");
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
