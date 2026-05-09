using ChatService.Dtos;
using ChatService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly IChatService _chat;

        public ConversationsController(IChatService chat) => _chat = chat;

        private string CurrentUserId =>
         User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
         ?? throw new InvalidOperationException("The authenticated user context is missing the NameIdentifier claim.");

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationDto dto)
        {
            var conv = await _chat.CreateConversationAsync(CurrentUserId, dto);
            return Ok(conv);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var convs = await _chat.GetUserConversationsAsync(CurrentUserId, pageNumber, pageSize);
            return Ok(convs);
        }

        [HttpGet("{convId}")]
        public async Task<IActionResult> Get(Guid convId)
        {
            var conv = await _chat.GetConversationAsync(convId, CurrentUserId);
            if (conv == null) return NotFound();
            return Ok(conv);
        }

        [HttpDelete("delete/{convId}")]
        public async Task<IActionResult> deleteConversationById(Guid convId)
        {
           var result =  await _chat.DeleteConversationAsync(convId, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{conversationId}/toggleBlock")]
        public async Task<IActionResult> ToggleBlock(Guid conversationId, [FromQuery] bool block)
        {
            var resp = await _chat.BlockOrUnBlockConversation(conversationId, CurrentUserId, block);
            if (!resp.Success) return BadRequest(resp);
            return Ok(resp);
        }

        [HttpPost("{convId}/participants")]
        public async Task<IActionResult> AddParticipant(Guid convId, [FromBody] IEnumerable<AddParticipantDto> dtos)
        {
            await _chat.AddParticipantsAsync(convId, CurrentUserId, dtos);
            return Ok(new { success = true, count = dtos.Count() });
        }

        [HttpDelete("{convId}/participants/{userId}")]
        public async Task<IActionResult> RemoveParticipant(Guid convId, string userId)
        {
            await _chat.RemoveParticipantAsync(convId, CurrentUserId, userId);
            return Ok(new { success = true });
        }

        [HttpDelete("{convId}/leave")]
        public async Task<IActionResult> Leave(Guid convId)
        {
            await _chat.RemoveParticipantAsync(convId, CurrentUserId, CurrentUserId);
            return Ok(new { success = true });
        }

        [HttpGet("userConversation/{otherUserId}")]
            public async Task<IActionResult> GetConversationByUserId(Guid otherUserId) {
            return Ok(await _chat.GetUserConversationByUserId(CurrentUserId, otherUserId));
        }
    }
}
