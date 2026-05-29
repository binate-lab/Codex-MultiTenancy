using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Queries
{
    public class GetTenantByIdentifierQuery : IRequest<IResponseWrapper>
    {
        public string TenantIdentifier { get; set; }
    }

    public class GetTenantByIdentifierQueryHandler : IRequestHandler<GetTenantByIdentifierQuery, IResponseWrapper>
    {
        private readonly ITenantService _tenantService;

        public GetTenantByIdentifierQueryHandler(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task<IResponseWrapper> Handle(GetTenantByIdentifierQuery request, CancellationToken cancellationToken)
        {
            var tenant = await _tenantService.GetTenantByIdentifierAsync(request.TenantIdentifier);
            if (tenant is not null)
            {
                return await ResponseWrapper<TenantResponse>.SuccessAsync(data: tenant);
            }
            return await ResponseWrapper<TenantResponse>.FailAsync(message: $"Aucune organisation avec l'identifiant '{request.TenantIdentifier}' trouvée!");
        }
    }
}
