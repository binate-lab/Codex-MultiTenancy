namespace App.Infrastructure.Services.Eleves
{
    // Client du microservice Scolarite.Api : liste des eleves d'une ecole (ScolariteDb),
    // pour alimenter la grille du formulaire « Scolarités ».
    public interface IScolariteEleveService
    {
        // Eleves d'une ecole (CodeEts), filtres optionnellement par annee scolaire.
        // Retourne une liste vide si le service est indisponible.
        Task<IReadOnlyList<EleveScolariteItem>> GetElevesAsync(string codeEts, string annee = null);
    }

    // Projection plate calquee sur EleveListeItem (Scolarite.Api/Eleves/Liste).
    public record EleveScolariteItem(
        string Matricule,
        string Telephone,
        string Nom,
        string Prenom,
        bool Inscrit,
        string Statut,
        string Niveau,
        string Classe,
        decimal FraisScolarite,
        decimal Solde);
}
