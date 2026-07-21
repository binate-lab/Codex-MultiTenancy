namespace App.Infrastructure.Services.Orange
{
    // Notification de paiement Orange Money (page de supervision « Paiements Orange »).
    public class PaiementOrangeItem
    {
        public Guid Id { get; set; }
        public string Matricule { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenoms { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DatePaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;   // EnAttente / Orphelin / Valide / Rejete
        public Guid? EleveId { get; set; }
        public Guid? VersementId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string? Note { get; set; }

        // Identité RÉELLE de l'élève rattaché (vide si orphelin) : c'est lui qui sera crédité.
        // Nom/Prenoms ci-dessus = ce qu'Orange a déclaré (peut différer → alerte côté page).
        public string EleveNomComplet { get; set; } = string.Empty;
        public string EleveMatricule { get; set; } = string.Empty;

        // Numéro Orange Money du payeur, déclaré par Orange (vide si non fourni).
        public string NumeroPayeur { get; set; } = string.Empty;

        // À qui appartient ce numéro : Pere / Mere / Tuteur / Inconnu (calculé au rattachement
        // en comparant le numéro aux téléphones des parents de l'élève).
        public string LienPayeur { get; set; } = string.Empty;

        // Décision de l'agent : "A" (Valider Auto) / "M" (Valider Manu) ; vide si non traité.
        public string ModeValidation { get; set; } = string.Empty;
    }

    public record PaiementOpResult(bool IsSuccessful, string? Error = null);

    public interface IPaiementOrangeService
    {
        // statut null => tous. Sinon filtre (EnAttente / Orphelin / Valide / Rejete).
        Task<IReadOnlyList<PaiementOrangeItem>> GetAsync(string? statut = null);
        Task<PaiementOpResult> ValiderAsync(Guid id);
        // Valider Manu (M) : retire le brouillon ; le caissier répartit lui-même le montant.
        Task<PaiementOpResult> ValiderManuAsync(Guid id);
        Task<PaiementOpResult> RattacherAsync(Guid id, string matricule);
        Task<PaiementOpResult> RejeterAsync(Guid id, string? note);
    }
}
