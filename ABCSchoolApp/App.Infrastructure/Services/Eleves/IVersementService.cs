namespace App.Infrastructure.Services.Eleves
{
    // Versements d'un eleve (module Versements de Scolarite.Api) : consultation du
    // detail + saisie (sous-form bleu ciel du form Access « Scolarités »).

    public class VersementDetailItem
    {
        public decimal Montant { get; set; }
        public DateTime DateVersement { get; set; }
        public string MoyenPaiement { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ReferencePaiement { get; set; } = string.Empty;
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

    public class VersementsEleveReponse
    {
        public ScolariteResume Resume { get; set; } = new();
        public List<VersementDetailItem> Versements { get; set; } = new();
        public List<EcheanceEleveItem> Echeancier { get; set; } = new();

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
            string nature, string moyenPaiement, string referenceOperation);

        // Recu de paiement PDF (situation du compte : versements + synthese + echeancier).
        // ecole = nom d'affichage de l'etablissement ; logoBase64 = logo ecole (data-URI
        // ou base64) affiche au centre de l'en-tete. null si indisponible.
        Task<byte[]> GetRecuPdfAsync(Guid eleveId, string ecole, string logoBase64);
    }
}
