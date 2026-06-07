using Domain.Enums;

namespace Application.Features.Certificats
{
    public class CertificatResponse
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; }
        public string UtilisateurId { get; set; }
        public string NomAppareil { get; set; }
        public string Description { get; set; }
        public string Empreinte { get; set; }
        public string NumeroSerie { get; set; }
        public DateTime EmisLe { get; set; }
        public DateTime ExpireLe { get; set; }
        public DateTime? RevoqueLe { get; set; }
        public string RaisonRevocation { get; set; }
        public StatutCertificat Statut { get; set; }
    }
}
