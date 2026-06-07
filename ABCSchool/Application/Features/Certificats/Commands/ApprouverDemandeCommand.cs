using Application.Wrappers;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class ApprouverDemandeCommand : IRequest<IResponseWrapper>
    {
        public Guid DemandeId { get; set; }
        public int DureeValiditeJours { get; set; } = 365;
    }

    public class ApprouverDemandeCommandHandler : IRequestHandler<ApprouverDemandeCommand, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public ApprouverDemandeCommandHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(ApprouverDemandeCommand request, CancellationToken cancellationToken)
        {
            var demande = await _certificatService.GetDemandeByIdAsync(request.DemandeId);

            if (demande is null)
                return await ResponseWrapper.FailAsync($"Demande '{request.DemandeId}' introuvable.");

            var result = await _certificatService.ApprouverDemandeAsync(
                request.DemandeId, request.DureeValiditeJours);

            return await ResponseWrapper<CertificatEmisResult>.SuccessAsync(data: result,
                "Demande approuvée. Certificat généré — téléchargez le PFX et transmettez le mot de passe séparément.");
        }
    }
}
