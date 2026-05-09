using ChatService.Dtos;
using ChatService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedService.DTOs;
using System;
using System.Security.Claims;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IChatService _chatService;

        public MessagesController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        // GET api/messages/history/{CoversationId}
        [HttpGet("history/{conversationId}")]
        public async Task<Response<MessagePageDto>> History(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var resp = await _chatService.GetMessagesAsync(CurrentUserId, conversationId, page, pageSize);
            return Response<MessagePageDto>.Ok(resp);
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageDto dto)
        {
            var msg = await _chatService.SendMessageAsync(CurrentUserId, dto);
            return Ok(msg);
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] MarkReadDto dto)
        {
            await _chatService.MarkAsReadAsync(CurrentUserId, dto);
            return Ok(new { success = true });
        }

        [HttpGet("message-read-list")]
        public async Task<Response<MessageReadUsersDto>> GetMessageReadUsersList(
            [FromQuery] Guid conversationId,
            [FromQuery] Guid messageId)
        {
            var result = await _chatService
                .GetMessageReadUsersListAsync(CurrentUserId, conversationId, messageId);

            return Response<MessageReadUsersDto>.Ok(result);
        }


        [HttpGet("unread-count/{conversationId}")]
        public async Task<IActionResult> UnreadCount(Guid conversationId)
        {
            var count = await _chatService.GetUnreadCountAsync(CurrentUserId, conversationId);
            return Ok(new { unread = count });
        }

        [HttpGet("unread")]
        public async Task<IActionResult> UnreadMessages()
        {
            var list = await _chatService.GetUnreadMessagesAsync(CurrentUserId);
            return Ok(list);
        }

        [HttpDelete("delete/{messageId}")]
        public async Task<IActionResult> Delete(Guid messageId)
        {
            var response = await _chatService.DeleteMessageAsync(CurrentUserId, messageId);
            if(!response)
            {
                return NotFound(new { error = "Message not found or you do not have permission to delete it." });
            }
            return Ok(new { success = true });
        }

        [HttpDelete("deleteUntil/{conversationId}")]
        public async Task<IActionResult> DeleteUntil(Guid conversationId)
        {
            var response = await _chatService.ClearAllMessagesUntilAsync(CurrentUserId, conversationId);
            if(!response)
            {
                return BadRequest(new { error = "Failed to Clear or You do not have permission to clear it." });
            }
            return Ok(new { success = true });
        }

        [HttpPut("update")]
        public async Task<IActionResult> EditMessage(EditMessageDto dto)
        {
            if(string.IsNullOrWhiteSpace(dto.NewContent))
            {
                return BadRequest(new { error = "New content cannot be empty." });
            }
            if (!Guid.TryParse(dto.MessageId, out Guid _))
            {
                return BadRequest(new { error = "Invalid message ID." });
            }

          var response =  await _chatService.EditMessageAsync(CurrentUserId, dto);
            if(response!=null)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(new { error = "Message not found or you do not have permission to edit it." });
            }
        }
    }
}
