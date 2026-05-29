using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Commands
{
    public class ActivateTenantCommand : IRequest<IResponseWrapper>
    {
        public string TenantIdentifier { get; set; }
    }

    public class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, IResponseWrapper>
    {
        private readonly ITenantService _tenantService;

        public ActivateTenantCommandHandler(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task<IResponseWrapper> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenantIdentifier = await _tenantService.ActivateAsync(request.TenantIdentifier);
            return await ResponseWrapper<string>.SuccessAsync(data: tenantIdentifier, "Activation Organisation reussie!");
        }
    }
}
