namespace App.Infrastructure.Services.Economat
{
    // Zone du barème de transport (module Economat de Scolarite.Api) : montants par mois pour
    // une zone donnée. Total calculé côté backend et recalculé localement après édition.
    public class ZoneTransportItem
    {
        public Guid Id { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;   // code (A1, B2…)
        public string NomZone { get; set; }                // nom convivial (Riviera…)
        public bool OK { get; set; } = true;
        public decimal Total { get; set; }
        public decimal Septembre { get; set; }
        public decimal Octobre { get; set; }
        public decimal Novembre { get; set; }
        public decimal Decembre { get; set; }
        public decimal Janvier { get; set; }
        public decimal Fevrier { get; set; }
        public decimal Mars { get; set; }
        public decimal Avril { get; set; }
        public decimal Mai { get; set; }

        public decimal TotalCalcule => Septembre + Octobre + Novembre + Decembre
                                     + Janvier + Fevrier + Mars + Avril + Mai;
    }

    // Resultat d'ecriture : Error porte le message metier du backend (409 doublon...).
    public record ZoneTransportOpResult(bool IsSuccessful, string Error = null);

    public interface IZoneTransportService
    {
        Task<IReadOnlyList<ZoneTransportItem>> GetZonesAsync(string annee = null);
        Task<ZoneTransportOpResult> CreateAsync(string anneeScolaire, string zone, string nomZone);
        Task<ZoneTransportOpResult> UpdateAsync(ZoneTransportItem zone);
        Task<ZoneTransportOpResult> DeleteAsync(Guid id);
    }
}
