using Application.Wrappers;
using Mapster;
using MediatR;

namespace Application.Features.AnneesScolaires.Queries
{
    // Renvoie l'année scolaire en cours (affichée dans l'espace école).
    public class GetAnneeEnCoursQuery : IRequest<IResponseWrapper>
    {
    }

    public class GetAnneeEnCoursQueryHandler : IRequestHandler<GetAnneeEnCoursQuery, IResponseWrapper>
    {
        private readonly IAnneeScolaireService _anneeScolaireService;

        public GetAnneeEnCoursQueryHandler(IAnneeScolaireService anneeScolaireService)
        {
            _anneeScolaireService = anneeScolaireService;
        }

        public async Task<IResponseWrapper> Handle(GetAnneeEnCoursQuery request, CancellationToken cancellationToken)
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();

            if (annee is null)
            {
                return await ResponseWrapper<int>.FailAsync("Aucune année scolaire en cours définie.");
            }

            return await ResponseWrapper<AnneeScolaireResponse>
                .SuccessAsync(data: annee.Adapt<AnneeScolaireResponse>());
        }
    }
}
