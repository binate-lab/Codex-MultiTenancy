using Application.Wrappers;
using Mapster;
using MediatR;

namespace Application.Features.Certificats.Queries
{
    public class GetDemandesPendantesQuery : IRequest<IResponseWrapper>
    {
    }

    public class GetDemandesPendantesQueryHandler : IRequestHandler<GetDemandesPendantesQuery, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public GetDemandesPendantesQueryHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(GetDemandesPendantesQuery request, CancellationToken cancellationToken)
        {
            var demandes = await _certificatService.GetDemandesPendantesAsync();

            if (demandes?.Count > 0)
                return await ResponseWrapper<List<DemandeResponse>>
                    .SuccessAsync(data: demandes.Adapt<List<DemandeResponse>>());

            return await ResponseWrapper<List<DemandeResponse>>.SuccessAsync(
                data: [], "Aucune demande en attente.");
        }
    }
}
