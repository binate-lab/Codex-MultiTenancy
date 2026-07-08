namespace App.Infrastructure.Services.Economat
{
    // Nature de versement configurable (module Economat de Scolarite.Api) : alimente la
    // deroulante « Nature du versement » (/scolarites) et la grille de configuration.
    public class NatureVersementItem
    {
        public int Id { get; set; }
        public int Ordre { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public bool OK { get; set; } = true;          // visible dans la deroulante de saisie
        public bool EstInscription { get; set; }      // nature qui rend l'eleve inscrit
    }

    // Resultat d'ecriture : Error porte le message metier du backend (409 doublon...).
    public record NatureOpResult(bool IsSuccessful, string Error = null);

    public interface INatureVersementService
    {
        Task<IReadOnlyList<NatureVersementItem>> GetNaturesAsync();
        Task<NatureOpResult> CreateAsync(string libelle, int? ordre, bool ok, bool estInscription);
        Task<NatureOpResult> UpdateAsync(NatureVersementItem nature);
        Task<NatureOpResult> DeleteAsync(int id);
    }
}
