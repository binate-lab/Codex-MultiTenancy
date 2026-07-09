using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Services.Eleves
{
    public interface IEleveService
    {
        Task<EleveCreationResult> CreateAsync(CreateEleveRequest request);

        // Le matricule national (forme canonique) est-il deja pris dans l'ecole ?
        // Sert au controle en direct (blur du champ). false si indisponible/erreur.
        Task<bool> MatriculeExisteAsync(string codeEts, string matricule);

        // Liste des eleves d'une ecole (CodeEts) depuis Pedagogie.Api (PedagogieDb) :
        // referentiel complet (page « Liste de classe »). Liste vide si indisponible.
        Task<IReadOnlyList<ElevePedagogieItem>> GetElevesAsync(string codeEts);

        // Enregistre la photo d'un eleve (base64 data URL) dans Pedagogie (Eleve.ImageFile).
        // Retourne true si l'enregistrement a reussi.
        Task<bool> MajPhotoAsync(Guid eleveId, string imageFile);
    }

    // Resultat simple : Eleves.Api renvoie 201 + { id, numOrdre }, sans ResponseWrapper.
    // NumOrdre = N° Inscription DEFINITIF attribue par Pedagogie (unique par ecole).
    public record EleveCreationResult(bool IsSuccessful, Guid Id, string Error, int NumOrdre = 0);

    // Projection d'une ligne « Liste de classe » : calque de Pedagogie.Application.Dtos.EleveDto
    // (GET /eleves). Statut arrive en ENTIER (StatutEleve : Aff=1, Naff=2) car Pedagogie n'a
    // pas de JsonStringEnumConverter — la conversion en libelle est faite a l'affichage.
    public record ElevePedagogieItem(
        Guid Id,
        int NumOrdre,
        string Matricule,
        string Nom,
        string Prenom,
        int Cycle,
        string Niveau,
        string Classe,
        string Serie,
        string Sexe,
        string LieuNaissance,
        string Nationalite,
        DateTime? DateNaissance,
        int Statut,
        bool IsInscrit,
        bool IsActif,
        string Telephone,
        string ImageFile,   // photo de l'élève (data URL / chemin / base64) — souvent vide tant que l'upload n'existe pas
        TuteurItem? Tuteur);   // correspondant / tuteur (nom + téléphones)

    // Sous-objet tuteur (calque partiel de ParentDto renvoyé par Pedagogie) : ce dont la
    // fiche a besoin (Telephone1 = tél., Telephone2 = WhatsApp).
    public record TuteurItem(string? Nom, string? Prenom, string? Telephone1, string? Telephone2);
}
