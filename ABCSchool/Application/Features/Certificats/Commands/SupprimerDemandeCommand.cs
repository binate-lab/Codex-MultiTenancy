using Application.Wrappers;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class SupprimerDemandeCommand : IRequest<IResponseWrapper>
    {
        public Guid DemandeId { get; set; }
        public string TenantId { get; set; }
        public bool EstRoot { get; set; }
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

            // Un tenant ne peut supprimer que ses propres demandes ; le Root peut tout supprimer.
            if (!request.EstRoot && demande.TenantId != request.TenantId)
                return await ResponseWrapper.FailAsync("Vous n'êtes pas autorisé à supprimer cette demande.");

            await _certificatService.SupprimerDemandeAsync(request.DemandeId);

            return await ResponseWrapper.SuccessAsync("Demande supprimée.");
        }
    }
}
