using Domain.Enums;

namespace Domain.Entities
{
    public class DemandeCertificat
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; }
        public string DemandeParAdminId { get; set; }
        public string NomAppareil { get; set; }
        public string Description { get; set; }
        public string? UtilisateurId { get; set; }
        public DateTime DemandeeLe { get; set; }
        public StatutDemande Statut { get; set; }
        public string? RaisonRejet { get; set; }
        public Guid? CertificatId { get; set; }
    }
}
