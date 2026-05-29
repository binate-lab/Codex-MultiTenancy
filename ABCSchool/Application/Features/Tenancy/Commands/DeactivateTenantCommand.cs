using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Commands
{
    public class DeactivateTenantCommand : IRequest<IResponseWrapper>
    {
        public string TenantIdentifier { get; set; }
    }

    public class DeactivateTenantCommandHandler : IRequestHandler<DeactivateTenantCommand, IResponseWrapper>
    {
        private readonly ITenantService _tenantService;

        public DeactivateTenantCommandHandler(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task<IResponseWrapper> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenantIdentifier = await _tenantService.DeactivateAsync(request.TenantIdentifier);

            return await ResponseWrapper<string>.SuccessAsync(data: tenantIdentifier, "La désactivation de l'organisation a reussi!");
        }
    }
}
