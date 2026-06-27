using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Services.Eleves
{
    public interface IEleveService
    {
        Task<EleveCreationResult> CreateAsync(CreateEleveRequest request);
    }

    // Resultat simple : Eleves.Api renvoie 201 + { id }, sans ResponseWrapper.
    public record EleveCreationResult(bool IsSuccessful, Guid Id, string Error);
}
