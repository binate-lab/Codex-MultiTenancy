using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Commands
{
    public class UpdateTenantSubscriptionCommand : IRequest<IResponseWrapper>
    {
        public UpdateTenantSubscriptionRequest UpdateTenantSubscription { get; set; }
    }

    public class UpdateTenantSubscriptionCommandHandler : IRequestHandler<UpdateTenantSubscriptionCommand, IResponseWrapper>
    {
        private readonly ITenantService _tenantService;

        public UpdateTenantSubscriptionCommandHandler(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task<IResponseWrapper> Handle(UpdateTenantSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var tenantIdentifier = await _tenantService.UpdateSubscriptionAsync(request.UpdateTenantSubscription);
            return await ResponseWrapper<string>.SuccessAsync(data: tenantIdentifier, "Mise à jour souscription de l'organisation effectuée avec succès");
        }
    }
}
