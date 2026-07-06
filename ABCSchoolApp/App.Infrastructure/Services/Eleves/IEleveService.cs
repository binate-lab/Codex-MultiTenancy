using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Services.Eleves
{
    public interface IEleveService
    {
        Task<EleveCreationResult> CreateAsync(CreateEleveRequest request);

        // Le matricule national (forme canonique) est-il deja pris dans l'ecole ?
        // Sert au controle en direct (blur du champ). false si indisponible/erreur.
        Task<bool> MatriculeExisteAsync(string codeEts, string matricule);
    }

    // Resultat simple : Eleves.Api renvoie 201 + { id, numOrdre }, sans ResponseWrapper.
    // NumOrdre = N° Inscription DEFINITIF attribue par Pedagogie (unique par ecole).
    public record EleveCreationResult(bool IsSuccessful, Guid Id, string Error, int NumOrdre = 0);
}
