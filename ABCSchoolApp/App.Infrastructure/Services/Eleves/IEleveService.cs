using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Services.Eleves
{
    public interface IEleveService
    {
        Task<EleveCreationResult> CreateAsync(CreateEleveRequest request);
    }

    // Resultat simple : Eleves.Api renvoie 201 + { id, numOrdre }, sans ResponseWrapper.
    // NumOrdre = N° Inscription DEFINITIF attribue par Pedagogie (unique par ecole).
    public record EleveCreationResult(bool IsSuccessful, Guid Id, string Error, int NumOrdre = 0);
}
