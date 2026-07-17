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

        // Persiste dans Pedagogie les editions en ligne de la grille « Liste de classe ».
        Task<bool> MajStatutAsync(Guid eleveId, string statut);
        Task<bool> MajClasseAsync(Guid eleveId, string classe);

        // Cycle + Niveau (edition reservee aux ecoles publiques). Remet la classe a vide cote
        // backend (le niveau change). Retourne false si refuse (400 : cycle/niveau invalide).
        Task<bool> MajCycleNiveauAsync(Guid eleveId, int cycle, string niveau);

        // Tuteur (correspondant) : Nom / Prenom / Tel1 (tel) / Tel2 (WhatsApp).
        Task<bool> MajTuteurAsync(Guid eleveId, string nom, string prenom, string telephone1, string telephone2);

        // Langue vivante 2 et Arts : editees en ligne dans la grille (deroulantes).
        Task<bool> MajLv2Async(Guid eleveId, string lv2);
        Task<bool> MajArtsAsync(Guid eleveId, string arts);

        // Operation en masse sur une liste d'eleves (panneau « Go » du bas de la grille).
        // operation ∈ { prenom-minuscule, prenom-majuscule, inscrire, desinscrire, lv2, arts,
        // serie } ; valeur requise pour lv2/arts/serie.
        Task<OperationEnMasseResult> OperationsEnMasseAsync(IReadOnlyList<Guid> ids, string operation, string? valeur);

        // Regenere les matricules de l'ecole courante (portee deduite du token cote serveur).
        // complet=false : recalcule les cles de controle (garde les chiffres) ; complet=true :
        // anonymise (chiffres neufs). Ecrit le journal [eleves].[LogsMatricules] cote Pedagogie.
        Task<RegenererMatriculesResult> RegenererMatriculesAsync(bool complet);
    }

    // Resultat de la regeneration en masse (Pedagogie renvoie { total, corriges, regenerations }).
    public record RegenererMatriculesResult(
        bool IsSuccessful, int Total, int Corriges, int Regenerations, string? Error = null);

    // Resultat d'une operation en masse (Pedagogie renvoie { count }).
    public record OperationEnMasseResult(bool IsSuccessful, int Count, string? Error = null);

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
        string LV_2,        // langue vivante 2 (Allemand / Espagnol) — éditable dans la grille
        string Arts,        // Arts Plastiques / Musique — éditable dans la grille
        TuteurItem? Tuteur);   // correspondant / tuteur (nom + téléphones)

    // Sous-objet tuteur (calque partiel de ParentDto renvoyé par Pedagogie) : ce dont la
    // fiche a besoin (Telephone1 = tél., Telephone2 = WhatsApp).
    public record TuteurItem(string? Nom, string? Prenom, string? Telephone1, string? Telephone2);
}
