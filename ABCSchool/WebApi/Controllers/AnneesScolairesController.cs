using Application.Features.AnneesScolaires.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class AnneesScolairesController : BaseApiController
    {
        // Année scolaire en cours du tenant connecté (simple authentification).
        [HttpGet("en-cours")]
        [Authorize]
        public async Task<IActionResult> GetAnneeEnCoursAsync()
        {
            var response = await Sender.Send(new GetAnneeEnCoursQuery());
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }
    }
}
