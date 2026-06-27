namespace Application.Features.AnneesScolaires
{
    public class AnneeScolaireResponse
    {
        public string Libelle { get; set; }
        public DateTime DebutAnneeScolaire { get; set; }
        public DateTime FinAnneeScolaire { get; set; }
        public bool AnneeEnCours { get; set; }
    }
}
