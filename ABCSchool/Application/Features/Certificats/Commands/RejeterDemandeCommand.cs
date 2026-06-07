using Application.Wrappers;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class RejeterDemandeCommand : IRequest<IResponseWrapper>
    {
        public Guid DemandeId { get; set; }
        public string Raison { get; set; }
    }

    public class RejeterDemandeCommandHandler : IRequestHandler<RejeterDemandeCommand, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public RejeterDemandeCommandHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(RejeterDemandeCommand request, CancellationToken cancellationToken)
        {
            var demande = await _certificatService.GetDemandeByIdAsync(request.DemandeId);

            if (demande is null)
                return await ResponseWrapper.FailAsync($"Demande '{request.DemandeId}' introuvable.");

            await _certificatService.RejeterDemandeAsync(request.DemandeId, request.Raison);

            return await ResponseWrapper.SuccessAsync("Demande rejetée.");
        }
    }
}
