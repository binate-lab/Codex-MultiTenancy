namespace App.Infrastructure.Services.Economat
{
    // Ligne du bareme d'echeancier (module Economat de Scolarite.Api) : montants par
    // mois pour un couple Niveau/Statut. Total est calcule cote backend (somme
    // d'Inscription a Mai) et recalcule localement apres chaque edition de cellule.
    public class ModaliteVersementItem
    {
        public Guid Id { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
        public string NiveauCode { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty; // « Aff » / « Naff »
        public decimal Total { get; set; }
        public decimal Inscription { get; set; }
        public decimal Septembre { get; set; }
        public decimal Octobre { get; set; }
        public decimal Novembre { get; set; }
        public decimal Decembre { get; set; }
        public decimal Janvier { get; set; }
        public decimal Fevrier { get; set; }
        public decimal Mars { get; set; }
        public decimal Avril { get; set; }
        public decimal Mai { get; set; }

        public decimal TotalCalcule => Inscription + Septembre + Octobre + Novembre + Decembre
                                     + Janvier + Fevrier + Mars + Avril + Mai;
    }

    // Resultat d'ecriture : Error porte le message metier du backend (409 doublon...).
    public record EcheancierOpResult(bool IsSuccessful, string Error = null);

    public interface IEcheancierService
    {
        Task<IReadOnlyList<ModaliteVersementItem>> GetModalitesAsync(string annee = null);
        Task<EcheancierOpResult> CreateModaliteAsync(string anneeScolaire, string niveauCode, string statut);
        Task<EcheancierOpResult> UpdateMontantsAsync(ModaliteVersementItem ligne);
        Task<EcheancierOpResult> DeleteModaliteAsync(Guid id);
    }
}
