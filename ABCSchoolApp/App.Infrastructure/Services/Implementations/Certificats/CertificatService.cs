using App.Infrastructure.Extensions;
using App.Infrastructure.Services.Certificats;
using System.Net.Http.Json;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Implementations.Certificats
{
    public class CertificatService : ICertificatService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public CertificatService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<IResponseWrapper<Guid>> SoumettreDemandeAsync(SoumettreDemandeRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.CertificatEndpoints.SoumettreDemandeUrl, request);
            return await response.WrapToResponse<Guid>();
        }

        public async Task<IResponseWrapper<List<DemandeResponse>>> GetMesDemandesAsync()
        {
            var response = await _httpClient.GetAsync(_apiSettings.CertificatEndpoints.MesDemandesUrl);
            return await response.WrapToResponse<List<DemandeResponse>>();
        }

        public async Task<IResponseWrapper<List<CertificatResponse>>> GetMesAppareilsAsync()
        {
            var response = await _httpClient.GetAsync(_apiSettings.CertificatEndpoints.MesAppareilsUrl);
            return await response.WrapToResponse<List<CertificatResponse>>();
        }

        public async Task<IResponseWrapper<List<DemandeResponse>>> GetDemandesPendantesAsync()
        {
            var response = await _httpClient.GetAsync(_apiSettings.CertificatEndpoints.DemandesPendantesUrl);
            return await response.WrapToResponse<List<DemandeResponse>>();
        }

        public async Task<IResponseWrapper<CertificatEmisResult>> ApprouverDemandeAsync(Guid demandeId, int dureeJours = 365)
        {
            var url = $"{_apiSettings.CertificatEndpoints.GetApprouver(demandeId)}?dureeJours={dureeJours}";
            var response = await _httpClient.PutAsJsonAsync(url, (object)null);
            return await response.WrapToResponse<CertificatEmisResult>();
        }

        public async Task<IResponseWrapper<string>> SupprimerDemandeAsync(Guid demandeId)
        {
            var response = await _httpClient.DeleteAsync(_apiSettings.CertificatEndpoints.GetSupprimer(demandeId));
            return await response.WrapToResponse<string>();
        }

        public async Task<IResponseWrapper<string>> ReactiverCertificatAsync(Guid certificatId)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.CertificatEndpoints.GetReactiver(certificatId), (object)null);
            return await response.WrapToResponse<string>();
        }

        public async Task<IResponseWrapper<string>> RejeterDemandeAsync(Guid demandeId, string raison)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.CertificatEndpoints.GetRejeter(demandeId), raison);
            return await response.WrapToResponse<string>();
        }

        public async Task<IResponseWrapper<string>> RevoquerCertificatAsync(Guid certificatId, string raison)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.CertificatEndpoints.GetRevoquer(certificatId), raison);
            return await response.WrapToResponse<string>();
        }
    }
}
