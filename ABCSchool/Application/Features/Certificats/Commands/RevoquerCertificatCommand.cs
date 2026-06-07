using Application.Wrappers;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class RevoquerCertificatCommand : IRequest<IResponseWrapper>
    {
        public Guid CertificatId { get; set; }
        public string Raison { get; set; }
    }

    public class RevoquerCertificatCommandHandler : IRequestHandler<RevoquerCertificatCommand, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public RevoquerCertificatCommandHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(RevoquerCertificatCommand request, CancellationToken cancellationToken)
        {
            var certificat = await _certificatService.GetCertificatByIdAsync(request.CertificatId);

            if (certificat is null)
                return await ResponseWrapper.FailAsync($"Certificat '{request.CertificatId}' introuvable.");

            await _certificatService.RevoquerCertificatAsync(request.CertificatId, request.Raison);

            return await ResponseWrapper.SuccessAsync(
                $"Certificat de '{certificat.NomAppareil}' révoqué. L'appareil n'a plus accès à l'application.");
        }
    }
}
