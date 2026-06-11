using Application.Wrappers;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class SupprimerDemandeCommand : IRequest<IResponseWrapper>
    {
        public Guid DemandeId { get; set; }
    }

    public class SupprimerDemandeCommandHandler : IRequestHandler<SupprimerDemandeCommand, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public SupprimerDemandeCommandHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(SupprimerDemandeCommand request, CancellationToken cancellationToken)
        {
            var demande = await _certificatService.GetDemandeByIdAsync(request.DemandeId);

            if (demande is null)
                return await ResponseWrapper.FailAsync($"Demande '{request.DemandeId}' introuvable.");

            await _certificatService.SupprimerDemandeAsync(request.DemandeId);

            return await ResponseWrapper.SuccessAsync("Demande supprimée.");
        }
    }
}
