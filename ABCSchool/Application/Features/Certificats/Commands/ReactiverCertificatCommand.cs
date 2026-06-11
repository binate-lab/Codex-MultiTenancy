using Application.Wrappers;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class ReactiverCertificatCommand : IRequest<IResponseWrapper>
    {
        public Guid CertificatId { get; set; }
    }

    public class ReactiverCertificatCommandHandler : IRequestHandler<ReactiverCertificatCommand, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public ReactiverCertificatCommandHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(ReactiverCertificatCommand request, CancellationToken cancellationToken)
        {
            var certificat = await _certificatService.GetCertificatByIdAsync(request.CertificatId);

            if (certificat is null)
                return await ResponseWrapper.FailAsync($"Certificat '{request.CertificatId}' introuvable.");

            await _certificatService.ReactiverCertificatAsync(request.CertificatId);

            return await ResponseWrapper.SuccessAsync(
                $"Certificat de '{certificat.NomAppareil}' réactivé. L'appareil a de nouveau accès à l'application.");
        }
    }
}
