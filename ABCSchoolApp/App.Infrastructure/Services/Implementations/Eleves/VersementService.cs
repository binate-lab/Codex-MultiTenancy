using App.Infrastructure.Services.Eleves;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Eleves
{
    // Client du module Versements de Scolarite.Api (base = ApiSettings:ScolariteApiUrl).
    // Le JWT ecole-scoped est propage par l'AuthenticationHeaderHandler.
    public class VersementService : IVersementService
    {
        private readonly HttpClient _httpClient;

        public VersementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<VersementsEleveReponse> GetVersementsAsync(Guid eleveId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<VersementsEleveReponse>($"eleves/{eleveId}/versements");
            }
            catch
            {
                return null;
            }
        }

        public async Task<VersementOpResult> CreateAsync(Guid eleveId, decimal montant, DateTime? date,
            string nature, string moyenPaiement, string referenceOperation,
            bool rame = false, bool tenueSport = false, bool carnetCorresp = false,
            bool macaron = false, string modeRame = null, int? mois = null)
        {
            try
            {
                var reponse = await _httpClient.PostAsJsonAsync($"eleves/{eleveId}/versements", new
                {
                    montant,
                    dateVersement = date,
                    nature,
                    moyenPaiement,
                    referenceOperation,
                    rame,
                    tenueSport,
                    carnetCorresp,
                    macaron,
                    modeRame,
                    mois,
                });

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        public async Task<VersementOpResult> UpdateAsync(Guid eleveId, Guid versementId, decimal montant,
            DateTime? date, string nature, string moyenPaiement, string referenceOperation,
            bool rame = false, bool tenueSport = false, bool carnetCorresp = false,
            bool macaron = false, string modeRame = null, int? mois = null)
        {
            try
            {
                var reponse = await _httpClient.PutAsJsonAsync(
                    $"eleves/{eleveId}/versements/{versementId}", new
                    {
                        montant,
                        dateVersement = date,
                        nature,
                        moyenPaiement,
                        referenceOperation,
                        rame,
                        tenueSport,
                        carnetCorresp,
                        macaron,
                        modeRame,
                        mois,
                    });

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        public async Task<VersementOpResult> DeleteAsync(Guid eleveId, Guid versementId)
        {
            try
            {
                var reponse = await _httpClient.DeleteAsync($"eleves/{eleveId}/versements/{versementId}");

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        public async Task<VersementOpResult> AddReductionAsync(Guid eleveId, string type,
            decimal? montant, decimal? pourcentage, string reference)
        {
            try
            {
                var reponse = await _httpClient.PostAsJsonAsync($"eleves/{eleveId}/reductions", new
                {
                    type,
                    montant,
                    pourcentage,
                    reference,
                });

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        public async Task<VersementOpResult> DeleteReductionAsync(Guid eleveId, Guid reductionId)
        {
            try
            {
                var reponse = await _httpClient.DeleteAsync($"eleves/{eleveId}/reductions/{reductionId}");

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        public async Task<VersementOpResult> SetTransportAsync(Guid eleveId, string zone)
        {
            try
            {
                var reponse = await _httpClient.PutAsJsonAsync($"eleves/{eleveId}/transport", new { zone });

                if (reponse.IsSuccessStatusCode)
                {
                    var data = await reponse.Content.ReadFromJsonAsync<VersementsEleveReponse>();
                    return new VersementOpResult(true, null, data);
                }

                var texte = await reponse.Content.ReadAsStringAsync();
                return new VersementOpResult(false,
                    string.IsNullOrWhiteSpace(texte) ? $"Erreur {(int)reponse.StatusCode}" : texte.Trim('"'));
            }
            catch (Exception ex)
            {
                return new VersementOpResult(false, $"Service indisponible : {ex.Message}");
            }
        }

        public async Task<byte[]> GetRecuPdfAsync(Guid eleveId, string ecole, string logoBase64, string ville, string anneeScolaire)
        {
            try
            {
                // POST : l'en-tete (nom d'ecole + logo base64) peut peser plusieurs Ko.
                var reponse = await _httpClient.PostAsJsonAsync(
                    $"eleves/{eleveId}/versements/recu",
                    new { ecole, logoBase64, ville, anneeScolaire });

                if (!reponse.IsSuccessStatusCode) return null;

                return await reponse.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }

        // Envoie le recu PDF au parent (Tuteur.Telephone1) via WhatsApp. Meme en-tete que
        // le recu PDF ; l'API genere le PDF, le publie temporairement et declenche Twilio.
        public async Task<(bool Ok, string Message)> EnvoyerRecuWhatsAppAsync(
            Guid eleveId, string ecole, string logoBase64, string ville, string anneeScolaire)
        {
            try
            {
                var reponse = await _httpClient.PostAsJsonAsync(
                    $"eleves/{eleveId}/versements/recu/whatsapp",
                    new { ecole, logoBase64, ville, anneeScolaire });

                if (reponse.IsSuccessStatusCode)
                    return (true, "Reçu envoyé par WhatsApp.");

                var erreur = await reponse.Content.ReadFromJsonAsync<WhatsAppErreurDto>();
                return (false, erreur?.message ?? "Échec de l'envoi WhatsApp.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        private sealed class WhatsAppErreurDto
        {
            public string? message { get; set; }
        }
    }
}
