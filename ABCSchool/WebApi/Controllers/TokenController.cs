using Application.Features.Identity.Tokens;
using Application.Features.Identity.Tokens.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity.Auth;
using Infrastructure.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class TokenController : BaseApiController
    {
        [HttpPost("login")]
        [AllowAnonymous]
        [TenantHeader]
        [OpenApiOperation("Utilisé pour obtenir jwt pour le login.")]
        public async Task<IActionResult> GetTokenAsync([FromBody] TokenRequest tokenRequest)
        {
            var response = await Sender.Send(new GetTokenQuery { TokenRequest = tokenRequest });

            if (response.IsSuccessful)
            {
                return Ok(response);
            } 
            return BadRequest(response);
        }

        [HttpPost("select-school")]
        [Authorize]
        [OpenApiOperation("Émet un JWT école-scoped pour l'école choisie (clic sur une carte d'école).")]
        public async Task<IActionResult> SelectSchoolAsync([FromBody] SelectSchoolRequest selectSchoolRequest)
        {
            var response = await Sender.Send(new SelectSchoolQuery { SelectSchool = selectSchoolRequest });

            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("refresh-token")]
        [OpenApiOperation("Utilisé pour un nouveau jwt de refresh token.")]
        [ShouldHavePermission(action: SchoolAction.RefreshToken, feature: SchoolFeature.Tokens)]
        public async Task<IActionResult> GetRefreshTokenAsync([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            var response = await Sender.Send(new GetRefreshTokenQuery { RefreshToken =  refreshTokenRequest });
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
