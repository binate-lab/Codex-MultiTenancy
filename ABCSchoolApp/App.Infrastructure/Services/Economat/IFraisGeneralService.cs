namespace App.Infrastructure.Services.Economat
{
    // Poste de Frais Generaux configurable (module Economat de Scolarite.Api) : grille de
    // configuration (/frais-generaux). Chaque poste OK au montant > 0 devient une ligne
    // d'echeance (groupe FraisGeneraux) a la generation de l'echeancier de l'eleve.
    public class FraisGeneralItem
    {
        public int Id { get; set; }
        public int Ordre { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public bool OK { get; set; } = true;          // ajoute a l'echeancier si coche
    }

    // Resultat d'ecriture : Error porte le message metier du backend (409 doublon...).
    public record FraisGeneralOpResult(bool IsSuccessful, string Error = null);

    // Resultat de « appliquer aux eleves existants » : nb d'eleves completes + nb de lignes ajoutees.
    public record AppliquerFgResult(int Eleves, int Lignes);

    public interface IFraisGeneralService
    {
        Task<IReadOnlyList<FraisGeneralItem>> GetPostesAsync();
        Task<FraisGeneralOpResult> CreateAsync(string libelle, decimal montant, int? ordre, bool ok);
        Task<FraisGeneralOpResult> UpdateAsync(FraisGeneralItem poste);
        Task<FraisGeneralOpResult> DeleteAsync(int id);

        // Ajoute les lignes FG manquantes aux eleves qui ont deja un echeancier. null si indisponible.
        Task<AppliquerFgResult> AppliquerAuxExistantsAsync();
    }
}
