using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Commands
{
    public class DeleteTenantCommand : IRequest<IResponseWrapper>
    {
        public string TenantIdentifier { get; set; }
    }

    public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, IResponseWrapper>
    {
        private readonly ITenantService _tenantService;

        public DeleteTenantCommandHandler(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task<IResponseWrapper> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
        {
            var tenantIdentifier = await _tenantService.DeleteTenantAsync(request.TenantIdentifier);
            return await ResponseWrapper<string>.SuccessAsync(data: tenantIdentifier, "La suppression de l'Ets a réussi!");
        }
    }
}
