using App.Infrastructure.Services.Economat;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Economat
{
    // Client du module Economat de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler.
    public class ZoneTransportService : IZoneTransportService
    {
        private readonly HttpClient _httpClient;

        public ZoneTransportService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<ZoneTransportItem>> GetZonesAsync(string annee = null)
        {
            try
            {
                var url = string.IsNullOrWhiteSpace(annee)
                    ? "economat/zones-transport"
                    : $"economat/zones-transport?annee={Uri.EscapeDataString(annee)}";
                var data = await _httpClient.GetFromJsonAsync<List<ZoneTransportItem>>(url);
                return data ?? new List<ZoneTransportItem>();
            }
            catch
            {
                return new List<ZoneTransportItem>();
            }
        }

        public Task<ZoneTransportOpResult> CreateAsync(string anneeScolaire, string zone, string nomZone)
            => EnvoyerAsync(() => _httpClient.PostAsJsonAsync("economat/zones-transport",
                new { anneeScolaire, zone, nomZone }));

        public Task<ZoneTransportOpResult> UpdateAsync(ZoneTransportItem zone)
            => EnvoyerAsync(() => _httpClient.PutAsJsonAsync($"economat/zones-transport/{zone.Id}", new
            {
                nomZone = zone.NomZone,
                ok = zone.OK,
                septembre = zone.Septembre,
                octobre = zone.Octobre,
                novembre = zone.Novembre,
                decembre = zone.Decembre,
                janvier = zone.Janvier,
                fevrier = zone.Fevrier,
                mars = zone.Mars,
                avril = zone.Avril,
                mai = zone.Mai,
            }));

        public Task<ZoneTransportOpResult> DeleteAsync(Guid id)
            => EnvoyerAsync(() => _httpClient.DeleteAsync($"economat/zones-transport/{id}"));

        private static async Task<ZoneTransportOpResult> EnvoyerAsync(Func<Task<HttpResponseMessage>> envoi)
        {
            try
            {
                var reponse = await envoi();
                if (reponse.IsSuccessStatusCode)
                    return new ZoneTransportOpResult(true);

                return new ZoneTransportOpResult(false, await LireErreurAsync(reponse));
            }
            catch (Exception ex)
            {
                return new ZoneTransportOpResult(false, $"Service indisponible : {ex.Message}");
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
