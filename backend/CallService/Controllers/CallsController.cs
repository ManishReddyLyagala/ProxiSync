using CallService.DTOs;
using CallService.Helpers;
using CallService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallService.Controllers
{
    [ApiController]
    [Route("api/calls")]
    [Authorize]
    public class CallsController: ControllerBase
    {
        private readonly ICallService _callService;

        public CallsController(ICallService callService)
        {
            _callService = callService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartCall([FromBody] StartCallDto dto)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _callService.StartCallAsync(dto, userId);
            return Ok(result);
        }

        [HttpPost("{callId:guid}/token")]
        public async Task<IActionResult> GetToken(Guid callId)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _callService.GenerateTokenAsync(callId, userId);
            return Ok(result);
        }

        [HttpPost("{callId:guid}/join")]
        public async Task<IActionResult> Join(Guid callId, [FromBody] JoinCallDto dto)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _callService.JoinCallAsync(callId, userId, dto);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpPost("{callId:guid}/reject")]
        public async Task<IActionResult> Reject(Guid callId)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _callService.RejectCallAsync(callId, userId);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpPost("{callId:guid}/leave")]
        public async Task<IActionResult> Leave(Guid callId)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _callService.LeaveCallAsync(callId, userId);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpPost("{callId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid callId)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _callService.CancelCallAsync(callId, userId);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpGet("{callId:guid}")]
        public async Task<IActionResult> GetCall(Guid callId)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var call = await _callService.GetCallAsync(callId, userId);
            return call != null ? Ok(call) : NotFound();
        }

        [HttpPatch("{callId:guid}/participant/media")]
        public async Task<IActionResult> UpdateMedia(Guid callId, [FromBody] ParticipantMediaDto dto)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _callService.UpdateParticipantMediaAsync(callId, userId, dto);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpPost("{callId:guid}/end")]
        public async Task<IActionResult> EndCall(Guid callId)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _callService.EndCallAsync(callId, userId);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        // GET: api/calls/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get ID from JWT
            var history = await _callService.GetHistoryAsync(userId);
            return Ok(history);
        }
    }
}
