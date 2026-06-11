using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Certificats
{
    public class SoumettreDemandeRequest
    {
        public string NomAppareil { get; set; }
        public string Description { get; set; }
        public string UtilisateurId { get; set; }
    }

    public class DemandeResponse
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; }
        public string NomAppareil { get; set; }
        public string Description { get; set; }
        public string UtilisateurId { get; set; }
        public DateTime DemandeeLe { get; set; }
        public int Statut { get; set; }
        public string StatutLibelle => Statut switch
        {
            1 => "En attente",
            2 => "Approuvée",
            3 => "Rejetée",
            4 => "Émise",
            _ => "Inconnu"
        };
        public string RaisonRejet { get; set; }
        public Guid? CertificatId { get; set; }
    }

    public class CertificatResponse
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; }
        public string NomAppareil { get; set; }
        public string Description { get; set; }
        public string Empreinte { get; set; }
        public DateTime EmisLe { get; set; }
        public DateTime ExpireLe { get; set; }
        public int Statut { get; set; }
        public string StatutLibelle => Statut switch
        {
            1 => "Actif",
            2 => "Révoqué",
            3 => "Expiré",
            _ => "Inconnu"
        };
        public DateTime? RevoqueLe { get; set; }
        public string RaisonRevocation { get; set; }
    }

    public class CertificatEmisResult
    {
        public Guid CertificatId { get; set; }
        public string PfxBase64 { get; set; }
        public string MotDePasse { get; set; }
        public string Empreinte { get; set; }
        public DateTime ExpireLe { get; set; }
    }

    public interface ICertificatService
    {
        Task<IResponseWrapper<Guid>> SoumettreDemandeAsync(SoumettreDemandeRequest request);
        Task<IResponseWrapper<List<DemandeResponse>>> GetMesDemandesAsync();
        Task<IResponseWrapper<List<CertificatResponse>>> GetMesAppareilsAsync();
        Task<IResponseWrapper<List<DemandeResponse>>> GetDemandesPendantesAsync();
        Task<IResponseWrapper<CertificatEmisResult>> ApprouverDemandeAsync(Guid demandeId, int dureeJours = 365);
        Task<IResponseWrapper<string>> RejeterDemandeAsync(Guid demandeId, string raison);
        Task<IResponseWrapper<string>> SupprimerDemandeAsync(Guid demandeId);
        Task<IResponseWrapper<string>> RevoquerCertificatAsync(Guid certificatId, string raison);
        Task<IResponseWrapper<string>> ReactiverCertificatAsync(Guid certificatId);
    }
}
