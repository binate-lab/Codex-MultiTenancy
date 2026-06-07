using Application.Features.Chat;
using Application.Features.Chat.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : BaseApiController
    {
        [HttpPost("send")]
        public async Task<IActionResult> SendAsync([FromBody] ChatRequest chatRequest)
        {
            var response = await Sender.Send(new SendChatMessageCommand { ChatRequest = chatRequest });
            if (response.IsSuccessful)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
