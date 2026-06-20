using Application.Features.Schools.Memberships;
using Application.Features.Schools.Memberships.Commands;
using Application.Features.Schools.Memberships.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class SchoolMembershipsController : BaseApiController
    {
        [HttpPost("assign")]
        [ShouldHavePermission(SchoolAction.Create, SchoolFeature.SchoolMemberships)]
        [OpenApiOperation("Affecte un utilisateur à une école avec un rôle.")]
        public async Task<IActionResult> AssignAsync([FromBody] AssignSchoolMembershipRequest request)
        {
            var response = await Sender.Send(new AssignSchoolMembershipCommand { Assign = request });

            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpDelete("revoke")]
        [ShouldHavePermission(SchoolAction.Delete, SchoolFeature.SchoolMemberships)]
        [OpenApiOperation("Retire l'affectation d'un utilisateur à une école pour un rôle.")]
        public async Task<IActionResult> RevokeAsync([FromBody] AssignSchoolMembershipRequest request)
        {
            var response = await Sender.Send(new RevokeSchoolMembershipCommand { Revoke = request });

            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpGet("user/{userId}/schools")]
        [ShouldHavePermission(SchoolAction.Read, SchoolFeature.SchoolMemberships)]
        [OpenApiOperation("Liste les écoles auxquelles un utilisateur est affecté.")]
        public async Task<IActionResult> GetUserSchoolsAsync(string userId)
        {
            var response = await Sender.Send(new GetUserSchoolsQuery { UserId = userId });

            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }
    }
}
