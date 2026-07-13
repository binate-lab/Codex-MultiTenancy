namespace App.Infrastructure.Services.Economat
{
    // Type de reduction configurable (module Economat de Scolarite.Api) : alimente la
    // deroulante « Type de reduction » (panneau Reductions de /scolarites) et la grille
    // de configuration (/types-reduction). Calque de NatureVersement, sans EstInscription.
    public class TypeReductionItem
    {
        public int Id { get; set; }
        public int Ordre { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public bool OK { get; set; } = true;          // visible dans la deroulante de saisie
    }

    // Resultat d'ecriture : Error porte le message metier du backend (409 doublon...).
    public record TypeReductionOpResult(bool IsSuccessful, string Error = null);

    public interface ITypeReductionService
    {
        Task<IReadOnlyList<TypeReductionItem>> GetTypesAsync();
        Task<TypeReductionOpResult> CreateAsync(string libelle, int? ordre, bool ok);
        Task<TypeReductionOpResult> UpdateAsync(TypeReductionItem type);
        Task<TypeReductionOpResult> DeleteAsync(int id);
    }
}
