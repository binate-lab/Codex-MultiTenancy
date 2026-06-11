using Domain.Entities;

namespace Application.Features.Certificats
{
    public interface ICertificatService
    {
        Task<Guid> SoumettreDemandeAsync(DemandeCertificat demande);
        Task<DemandeCertificat> GetDemandeByIdAsync(Guid demandeId);
        Task<List<DemandeCertificat>> GetDemandesByTenantAsync(string tenantId);
        Task<List<DemandeCertificat>> GetDemandesPendantesAsync();
        Task<CertificatEmisResult> ApprouverDemandeAsync(Guid demandeId, int dureeValiditeJours);
        Task RejeterDemandeAsync(Guid demandeId, string raison);
        Task SupprimerDemandeAsync(Guid demandeId);
        Task<List<CertificatAppareil>> GetCertificatsByTenantAsync(string tenantId);
        Task<CertificatAppareil> GetCertificatByIdAsync(Guid certificatId);
        Task RevoquerCertificatAsync(Guid certificatId, string raison);
        Task ReactiverCertificatAsync(Guid certificatId);
        Task<CertificatAppareil> GetByEmpreinteAsync(string empreinte);
    }
}
