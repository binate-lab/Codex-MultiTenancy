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
    }

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
        decimal Solde);
}
