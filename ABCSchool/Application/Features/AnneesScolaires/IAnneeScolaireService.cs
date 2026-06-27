using Domain.Entities;

namespace Application.Features.AnneesScolaires
{
    public interface IAnneeScolaireService
    {
        // Année scolaire active du tenant (ligne où AnneeEnCours = true). Null si aucune.
        Task<AnneeScolaire> GetAnneeEnCoursAsync();
    }
}
