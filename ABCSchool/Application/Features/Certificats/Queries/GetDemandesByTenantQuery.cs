using Application.Wrappers;
using Mapster;
using MediatR;

namespace Application.Features.Certificats.Queries
{
    public class GetDemandesByTenantQuery : IRequest<IResponseWrapper>
    {
        public string TenantId { get; set; }
    }

    public class GetDemandesByTenantQueryHandler : IRequestHandler<GetDemandesByTenantQuery, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public GetDemandesByTenantQueryHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(GetDemandesByTenantQuery request, CancellationToken cancellationToken)
        {
            var demandes = await _certificatService.GetDemandesByTenantAsync(request.TenantId);

            if (demandes?.Count > 0)
                return await ResponseWrapper<List<DemandeResponse>>
                    .SuccessAsync(data: demandes.Adapt<List<DemandeResponse>>());

            return await ResponseWrapper<List<DemandeResponse>>.SuccessAsync(
                data: [], "Aucune demande pour ce tenant.");
        }
    }
}
