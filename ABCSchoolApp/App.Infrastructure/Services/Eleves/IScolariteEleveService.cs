namespace App.Infrastructure.Services.Eleves
{
    // Client du microservice Scolarite.Api : liste des eleves d'une ecole (ScolariteDb),
    // pour alimenter la grille du formulaire « Scolarités ».
    public interface IScolariteEleveService
    {
        // Eleves d'une ecole (CodeEts), filtres optionnellement par annee scolaire.
        // Retourne une liste vide si le service est indisponible.
        Task<IReadOnlyList<EleveScolariteItem>> GetElevesAsync(string codeEts, string annee = null);

        // Met a jour le telephone du correspondant (colonne editable de la grille).
        // Retourne true si l'enregistrement a reussi.
        Task<bool> MajTelephoneCorrespondantAsync(Guid eleveId, string telephone);

        // Met a jour le CodeParent (fratrie) de l'eleve (colonne editable de la grille).
        // Retourne true si l'enregistrement a reussi.
        Task<bool> MajCodeParentAsync(Guid eleveId, string codeParent);

        // Etat des versements d'une PERIODE [dateDebut, dateFin] pour l'ecole, groupe par niveau
        // (defaut : aujourd'hui pour les deux ; fin = debut si absente). Alimente l'apercu/
        // impression « Versements entre deux dates ». Liste vide si indispo.
        Task<IReadOnlyList<VersementsJourNiveauItem>> GetVersementsPeriodeAsync(
            string codeEts, DateTime? dateDebut = null, DateTime? dateFin = null);
    }

    // Calques des DTOs de Scolarite.Api (VersementsDuJourEndpoint).
    public record VersementJourLigneItem(
        int Numero, string NomPrenoms, string Classe, decimal Montant, string Nature,
        string Mode, decimal TotalFrais, decimal TotalVerse, decimal Reste, string Auteur,
        bool Rame);

    public record VersementsJourNiveauItem(
        string Niveau, List<VersementJourLigneItem> Lignes, decimal TotalMontant, int NbRecu);

    // Projection plate calquee sur EleveListeItem (Scolarite.Api/Eleves/Liste).
    // Id = EleveId cote Scolarite, requis pour les endpoints versements.
    public record EleveScolariteItem(
        Guid Id,
        string Matricule,
        string Telephone,
        string Nom,
        string Prenom,
        bool Actif,
        bool Inscrit,
        string Statut,
        string Niveau,
        string Classe,
        decimal FraisScolarite,
        decimal Solde,
        int NumOrdre,    // N° Inscription (unique par ecole)
        string ZoneTransport,   // zone de transport de l'élève (null = pas de transport)
        string CodeParent);     // code parent (fratrie) — editable
}
