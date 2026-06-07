using Application.Pipelines;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using Mapster;
using MediatR;

namespace Application.Features.Certificats.Commands
{
    public class SoumettreDemandeCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public string TenantId { get; set; }
        public string DemandeParAdminId { get; set; }
        public SoumettreDemandeRequest Demande { get; set; }
    }

    public class SoumettreDemandeCommandHandler : IRequestHandler<SoumettreDemandeCommand, IResponseWrapper>
    {
        private readonly ICertificatService _certificatService;

        public SoumettreDemandeCommandHandler(ICertificatService certificatService)
        {
            _certificatService = certificatService;
        }

        public async Task<IResponseWrapper> Handle(SoumettreDemandeCommand request, CancellationToken cancellationToken)
        {
            var demande = request.Demande.Adapt<DemandeCertificat>();
            demande.TenantId = request.TenantId;
            demande.DemandeParAdminId = request.DemandeParAdminId;
            demande.DemandeeLe = DateTime.UtcNow;
            demande.Statut = StatutDemande.EnAttente;

            var demandeId = await _certificatService.SoumettreDemandeAsync(demande);

            return await ResponseWrapper<Guid>.SuccessAsync(data: demandeId,
                "Demande d'autorisation d'appareil soumise avec succès. En attente de validation.");
        }
    }
}
