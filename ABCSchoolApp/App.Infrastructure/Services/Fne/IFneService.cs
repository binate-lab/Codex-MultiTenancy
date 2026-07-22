namespace App.Infrastructure.Services.Fne
{
    // Paramétrage FNE (Facture Normalisée Électronique DGI) de l'école : identifiants
    // déclarés à la DGI + libellés fixes de la facture. Une seule ligne par école (upsert).
    public class ParametreFneDto
    {
        public string Ncc { get; set; } = string.Empty;
        public string PointOfSale { get; set; } = string.Empty;
        public string Establishment { get; set; } = string.Empty;
        public string Template { get; set; } = "B2C";
        public string? CommercialMessage { get; set; }
        public string? Footer { get; set; }
        public string? TaxeParDefaut { get; set; }
        public bool Actif { get; set; } = true;
    }

    // Ligne de la grille de suivi des certifications (une facture = un versement validé).
    public class FactureFneItem
    {
        public Guid Id { get; set; }
        public Guid VersementId { get; set; }
        public Guid EleveId { get; set; }
        public string TypeFacture { get; set; } = "sale";
        public string Statut { get; set; } = string.Empty;   // EnAttente / Certifiee / Echec
        public string? Reference { get; set; }
        public DateTime? DateCertification { get; set; }
        public int Tentatives { get; set; }
        public string? Erreur { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record FneOpResult(bool IsSuccessful, string? Error = null);

    public interface IFneService
    {
        // Null si l'école n'est pas encore enrôlée à la FNE.
        Task<ParametreFneDto?> GetParametresAsync();
        Task<FneOpResult> SaveParametresAsync(ParametreFneDto parametres);

        // statut : null = toutes ; sinon EnAttente / Certifiee / Echec.
        Task<IReadOnlyList<FactureFneItem>> GetFacturesAsync(string? statut);
        Task<FneOpResult> RelancerAsync(Guid factureId);
    }
}
