namespace App.Infrastructure.Services.Eleves
{
    // Versements d'un eleve (module Versements de Scolarite.Api) : consultation du
    // detail + saisie (sous-form bleu ciel du form Access « Scolarités »).

    public class VersementDetailItem
    {
        // Identifiant du versement — cible des operations Modifier / Supprimer.
        public Guid Id { get; set; }
        public decimal Montant { get; set; }
        public DateTime DateVersement { get; set; }
        public string MoyenPaiement { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ReferencePaiement { get; set; } = string.Empty;

        // Articles/fournitures rattaches au versement + mois couvert (1-12, null = non renseigne).
        public bool Rame { get; set; }
        public bool TenueSport { get; set; }
        public bool CarnetCorresp { get; set; }
        public bool Macaron { get; set; }
        public string ModeRame { get; set; }
        public int? Mois { get; set; }
    }

    // Bloc de synthese (Total frais / Payé / Reste / Crédit) renvoye avec le detail.
    public class ScolariteResume
    {
        public decimal FraisScolarite { get; set; }
        public decimal TotalReduction { get; set; }
        public decimal TotalVerse { get; set; }
        public decimal Reste { get; set; }
        public decimal Credit { get; set; }
    }

    // Ligne de l'echeancier individuel (Inscription puis Septembre -> Mai),
    // avec statut Paye / Partiel / NonPaye calcule cote backend.
    public class EcheanceEleveItem
    {
        public string Libelle { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal Reste { get; set; }
        public string Statut { get; set; } = string.Empty;
        public DateTime DateEcheance { get; set; }
        public bool EnRetard { get; set; }
    }

    // Une reduction accordee a l'eleve (grille du panneau Reductions).
    // Pourcentage non null => saisie en % de la scolarite (affichage « 10 % »).
    public class ReductionDetailItem
    {
        public Guid Id { get; set; }
        public string Motif { get; set; } = string.Empty;   // libelle du type de reduction
        public decimal Montant { get; set; }
        public decimal? Pourcentage { get; set; }
        public string? Reference { get; set; }               // justificatif libre (n° arrêté, nom membre…)
        public DateTime Date { get; set; }
    }

    public class VersementsEleveReponse
    {
        public ScolariteResume Resume { get; set; } = new();
        public List<VersementDetailItem> Versements { get; set; } = new();
        public List<EcheanceEleveItem> Echeancier { get; set; } = new();
        public List<ReductionDetailItem> Reductions { get; set; } = new();

        // Zone de transport de l'élève (null s'il ne prend pas le transport) : pilote le
        // sélecteur Transport de la fiche élève.
        public string ZoneTransport { get; set; }

        // Statut inscription de l'eleve (rafraichit les cases Actif/Inscrit de la grille
        // apres un versement d'inscription).
        public bool Actif { get; set; }
        public bool Inscrit { get; set; }
    }

    // Resultat d'ecriture : Error porte le message metier du backend ; Data = etat
    // rafraichi (resume + detail) apres l'enregistrement.
    public record VersementOpResult(bool IsSuccessful, string Error = null, VersementsEleveReponse Data = null);

    public interface IVersementService
    {
        // Detail + resume des versements d'un eleve. null si indisponible.
        Task<VersementsEleveReponse> GetVersementsAsync(Guid eleveId);

        // Enregistre un versement (valide + impute sur l'echeancier cote backend).
        Task<VersementOpResult> CreateAsync(Guid eleveId, decimal montant, DateTime? date,
            string nature, string moyenPaiement, string referenceOperation,
            bool rame = false, bool tenueSport = false, bool carnetCorresp = false,
            bool macaron = false, string modeRame = null, int? mois = null);

        // Modifie un versement existant (le backend rejoue toute l'imputation).
        Task<VersementOpResult> UpdateAsync(Guid eleveId, Guid versementId, decimal montant, DateTime? date,
            string nature, string moyenPaiement, string referenceOperation,
            bool rame = false, bool tenueSport = false, bool carnetCorresp = false,
            bool macaron = false, string modeRame = null, int? mois = null);

        // Supprime DEFINITIVEMENT un versement (le backend rejoue toute l'imputation).
        Task<VersementOpResult> DeleteAsync(Guid eleveId, Guid versementId);

        // Accorde une reduction : type (libelle du referentiel) + montant fixe OU pourcentage
        // (base = mensualites de scolarite). Le backend impute depuis la fin de l'echeancier.
        Task<VersementOpResult> AddReductionAsync(Guid eleveId, string type, decimal? montant, decimal? pourcentage, string reference);

        // Annule une reduction (le backend restaure les montants de l'echeancier).
        Task<VersementOpResult> DeleteReductionAsync(Guid eleveId, Guid reductionId);

        // Rattache l'eleve a une zone de transport (copie les montants mensuels dans son
        // echeancier) ou le retire (zone null/vide). Renvoie l'etat rafraichi.
        Task<VersementOpResult> SetTransportAsync(Guid eleveId, string zone);

        // Recu de paiement PDF (situation du compte : versements + synthese + echeancier).
        // ecole = nom d'affichage de l'etablissement ; logoBase64 = logo ecole (data-URI
        // ou base64) affiche au centre de l'en-tete. null si indisponible.
        Task<byte[]> GetRecuPdfAsync(Guid eleveId, string ecole, string logoBase64, string ville, string anneeScolaire);

        // Envoie le reçu PDF au parent via WhatsApp (Twilio). Renvoie (succès, message à afficher).
        Task<(bool Ok, string Message)> EnvoyerRecuWhatsAppAsync(Guid eleveId, string ecole, string logoBase64, string ville, string anneeScolaire);
    }
}
