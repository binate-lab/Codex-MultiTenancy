using Application.Features.Certificats;
using Application.Features.Certificats.Commands;
using Application.Features.Certificats.Queries;
using Application.Features.Identity.Users;
using Infrastructure.Constants;
using Infrastructure.Identity.Auth;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class CertificatsController : BaseApiController
    {
        private readonly ICurrentUserService _currentUser;

        public CertificatsController(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        // ─── Admin tenant ────────────────────────────────────────────────────────

        [HttpPost("demandes/soumettre")]
        [ShouldHavePermission(SchoolAction.Update, SchoolFeature.Certificats)]
        public async Task<IActionResult> SoumettreDemandeAsync([FromBody] SoumettreDemandeRequest demande)
        {
            var response = await Sender.Send(new SoumettreDemandeCommand
            {
                TenantId = _currentUser.GetUserTenant(),
                DemandeParAdminId = _currentUser.GetUserId(),
                Demande = demande
            });
            return response.IsSuccessful ? Ok(response) : BadRequest(response);
        }

        [HttpGet("mes-demandes")]
        [ShouldHavePermission(SchoolAction.Update, SchoolFeature.Certificats)]
        public async Task<IActionResult> GetMesDemandesAsync()
        {
            var response = await Sender.Send(new GetDemandesByTenantQuery
            {
                TenantId = _currentUser.GetUserTenant()
            });
            return response.IsSuccessful ? Ok(response) : NotFound(response);
        }

        [HttpGet("mes-appareils")]
        [ShouldHavePermission(SchoolAction.Update, SchoolFeature.Certificats)]
        public async Task<IActionResult> GetMesAppareilsAsync()
        {
            var response = await Sender.Send(new GetCertificatsByTenantQuery
            {
                TenantId = _currentUser.GetUserTenant()
            });
            return response.IsSuccessful ? Ok(response) : NotFound(response);
        }

        // ─── Root (Keita & équipe) ───────────────────────────────────────────────

        [HttpGet("demandes/en-attente")]
        [ShouldHavePermission(SchoolAction.Read, SchoolFeature.Certificats)]
        public async Task<IActionResult> GetDemandesPendantesAsync()
        {
            var response = await Sender.Send(new GetDemandesPendantesQuery());
            return response.IsSuccessful ? Ok(response) : NotFound(response);
        }

        [HttpPut("demandes/{demandeId}/approuver")]
        [ShouldHavePermission(SchoolAction.Create, SchoolFeature.Certificats)]
        public async Task<IActionResult> ApprouverDemandeAsync(Guid demandeId, [FromQuery] int dureeJours = 365)
        {
            var response = await Sender.Send(new ApprouverDemandeCommand
            {
                DemandeId = demandeId,
                DureeValiditeJours = dureeJours
            });
            return response.IsSuccessful ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("demandes/{demandeId}")]
        [ShouldHavePermission(SchoolAction.Update, SchoolFeature.Certificats)]
        public async Task<IActionResult> SupprimerDemandeAsync(Guid demandeId)
        {
            var tenantId = _currentUser.GetUserTenant();
            var response = await Sender.Send(new SupprimerDemandeCommand
            {
                DemandeId = demandeId,
                TenantId = tenantId,
                EstRoot = tenantId == TenancyConstants.Root.Identifier
            });
            return response.IsSuccessful ? Ok(response) : BadRequest(response);
        }

        [HttpPut("demandes/{demandeId}/rejeter")]
        [ShouldHavePermission(SchoolAction.Create, SchoolFeature.Certificats)]
        public async Task<IActionResult> RejeterDemandeAsync(Guid demandeId, [FromBody] string raison)
        {
            var response = await Sender.Send(new RejeterDemandeCommand
            {
                DemandeId = demandeId,
                Raison = raison
            });
            return response.IsSuccessful ? Ok(response) : BadRequest(response);
        }

        [HttpPut("{certificatId}/reactiver")]
        [ShouldHavePermission(SchoolAction.Create, SchoolFeature.Certificats)]
        public async Task<IActionResult> ReactiverCertificatAsync(Guid certificatId)
        {
            var response = await Sender.Send(new ReactiverCertificatCommand { CertificatId = certificatId });
            return response.IsSuccessful ? Ok(response) : BadRequest(response);
        }

        [HttpPut("{certificatId}/revoquer")]
        [ShouldHavePermission(SchoolAction.Delete, SchoolFeature.Certificats)]
        public async Task<IActionResult> RevoquerCertificatAsync(Guid certificatId, [FromBody] string raison)
        {
            var response = await Sender.Send(new RevoquerCertificatCommand
            {
                CertificatId = certificatId,
                Raison = raison
            });
            return response.IsSuccessful ? Ok(response) : BadRequest(response);
        }
    }
}
