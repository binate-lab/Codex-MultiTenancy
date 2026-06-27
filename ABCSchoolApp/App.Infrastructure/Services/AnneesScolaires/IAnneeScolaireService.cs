using TrajanEcole.Shared.Library.Models.Responses.AnneesScolaires;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.AnneesScolaires
{
    public interface IAnneeScolaireService
    {
        Task<IResponseWrapper<AnneeScolaireResponse>> GetAnneeEnCoursAsync();
    }
}
