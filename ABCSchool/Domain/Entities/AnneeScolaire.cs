namespace Domain.Entities
{
    public class AnneeScolaire
    {
        // Clé primaire : libellé de l'année scolaire (ex : "2025-2026"). Colonne SQL : "AnneeScolaire".
        public string Libelle { get; set; }

        public DateTime DebutAnneeScolaire { get; set; }
        public DateTime FinAnneeScolaire { get; set; }

        // Nullable : absent de l'ancien système Access (CAMA), saisi ultérieurement.
        public DateTime? FinSemestre1 { get; set; }
        public DateTime? FinSemestre2 { get; set; }

        public DateTime? FinTrimestre1 { get; set; }
        public DateTime? FinTrimestre2 { get; set; }

        public bool AnneeEnCours { get; set; }

        public int DelaiExclusion { get; set; }

        public string FinEncaissement { get; set; }
    }
}
