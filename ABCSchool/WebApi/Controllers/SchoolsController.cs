using Application.Features.Schools;
using Application.Features.Schools.Commands;
using Application.Features.Schools.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class SchoolsController : BaseApiController
    {
        [HttpPost("add")]
        [ShouldHavePermission(SchoolAction.Create, SchoolFeature.Schools)]
        public async Task<IActionResult> CreateSchoolAsync(
            [FromBody] CreateSchoolRequest createSchool,
            [FromServices] IWebHostEnvironment env)
        {
            var response = await Sender.Send(new CreateSchoolCommand { CreateSchool = createSchool });
            if (response.IsSuccessful)
            {
                // Crée le dossier de l'école sous wwwroot (pour ses futurs fichiers : logo, fond, etc.).
                EnsureSchoolFolder(env, createSchool.NomCourtEts);
                return Ok(response);
            }
            return BadRequest(response);
        }

        // Crée wwwroot/{NomCourtEts}. Échec silencieux : ne doit pas faire échouer la création de l'école.
        private static void EnsureSchoolFolder(IWebHostEnvironment env, string nomCourtEts)
        {
            if (string.IsNullOrWhiteSpace(nomCourtEts))
            {
                return;
            }

            try
            {
                var webRoot = env.WebRootPath;
                if (string.IsNullOrEmpty(webRoot))
                {
                    webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
                }

                var safeName = string.Concat(nomCourtEts.Trim().Split(Path.GetInvalidFileNameChars()));
                if (string.IsNullOrWhiteSpace(safeName))
                {
                    return;
                }

                Directory.CreateDirectory(Path.Combine(webRoot, safeName));
            }
            catch
            {
                // Volontairement ignoré.
            }
        }

        [HttpPut("update")]
        [ShouldHavePermission(SchoolAction.Update, SchoolFeature.Schools)]
        public async Task<IActionResult> UpdateSchoolAsync([FromBody] UpdateSchoolRequest updateSchool)
        {
            var response = await Sender.Send(new UpdateSchoolCommand { UpdateSchool = updateSchool });
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        [HttpDelete("{schoolId}")]
        [ShouldHavePermission(SchoolAction.Delete, SchoolFeature.Schools)]
        public async Task<IActionResult> DeleteSchoolAsync(int schoolId)
        {
            var response = await Sender.Send(new DeleteSchoolCommand { SchoolId = schoolId });
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        [HttpGet("by-id/{schoolId}")]
        [ShouldHavePermission(SchoolAction.Read, SchoolFeature.Schools)]
        public async Task<IActionResult> GetSchoolByIdAsync(int schoolId)
        {
            var response = await Sender.Send(new GetSchoolByIdQuery { SchoolId = schoolId });
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        [HttpGet("by-name/{name}")]
        [ShouldHavePermission(SchoolAction.Read, SchoolFeature.Schools)]
        public async Task<IActionResult> GetSchoolByNameAsync(string name)
        {
            var response = await Sender.Send(new GetSchoolByNameQuery { Name = name });
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        [HttpGet("all")]
        [ShouldHavePermission(SchoolAction.Read, SchoolFeature.Schools)]
        public async Task<IActionResult> GetAllSchoolsAsync()
        {
            var response = await Sender.Send(new GetSchoolsQuery());
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        // Écoles de l'utilisateur connecté (cartes d'accueil) : simple authentification,
        // sans permission admin. Admin tenant-wide => toutes ; sinon => ses affectations.
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMySchoolsAsync()
        {
            var response = await Sender.Send(new GetMySchoolsQuery());
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return NotFound(response);
        }
    }
}
