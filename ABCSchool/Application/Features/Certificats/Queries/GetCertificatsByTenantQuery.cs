using Application.Wrappers;
using Mapster;
using MediatR;

namespace Application.Features.Certificats.Queries
{
    public class GetCertificatsByTenantQuery : IRequest<IResponseWrapper>
    {
        public string TenantId { get; set; }
    }

    public class GetCertificatsByTenantQueryHandler : IRequestHandler<GetCertificatsByTenantQuery, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public GetCertificatsByTenantQueryHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(GetCertificatsByTenantQuery request, CancellationToken cancellationToken)
        {
            var certificats = await _certificatService.GetCertificatsByTenantAsync(request.TenantId);

            if (certificats?.Count > 0)
                return await ResponseWrapper<List<CertificatResponse>>
                    .SuccessAsync(data: certificats.Adapt<List<CertificatResponse>>());

            return await ResponseWrapper<List<CertificatResponse>>.SuccessAsync(
                data: [], "Aucun appareil autorisé pour ce tenant.");
        }
    }
}
