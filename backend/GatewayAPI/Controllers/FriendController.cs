using GatewayAPI.Interfaces;
using GatewayAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedService.DTOs;
using SharedService.Models;
using System.Security.Claims;
using GatewayAPI.DTOs;

namespace GatewayAPI.Controllers
{
    [ApiController]
    [Route("api/friend")]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly IChatClient _chatClient;

        public FriendController(IFriendService friendService, IChatClient chatClient)
        {
            _friendService = friendService;
            _chatClient = chatClient;
        }

        // POST api/friend/request/{toUserId}
        [HttpPost("request/{toUserId}")]
        public async Task<IActionResult> SendRequest(string toUserId, [FromBody] SendFriendRequestBody body)
        {
            var fromUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(fromUserId)) return Unauthorized(Response<string>.Fail("Invalid user"));

            var resp = await _friendService.SendRequestAsync(fromUserId, toUserId, body?.Message);
            return resp.Success ? Ok(resp) : BadRequest(resp);
        }

        // POST api/friend/respond/{requestId}
        [HttpPost("respond/{requestId}")]
        public async Task<IActionResult> Respond(Guid requestId, [FromBody] RespondBody body)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(Response<string>.Fail("Invalid user"));

            var resp = await _friendService.RespondRequestAsync(requestId, userId, body.Accept);
            if(resp.Success && resp?.Data?.Status == FriendRequestStatus.Accepted)
            {
                var chatDto = new CreateConversationDto
                {
                    Type = "direct",
                    ParticipantIds = new List<string> { resp.Data.FromUserId }
                };

                // This call will now manually find and forward the token
                await _chatClient.CreateConversationAsync(chatDto);
            }
            return resp.Success ? Ok(resp) : BadRequest(resp);
        }

        // GET api/friend/pending
        [HttpGet("respond/recived")]
        public async Task<IActionResult> FriendRequestRecivedList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _friendService.GetPendingRequestsAsync(userId);
            return list.Success ? Ok(list) : BadRequest(list);
        }

        // GET api/friend/sent
        [HttpGet("request/sent")]
        public async Task<IActionResult> FriendRequestSentList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _friendService.ListSentRequestsAsync(userId);
            return list.Success ? Ok(list) : BadRequest(list);
        }

        // GET api/friend/contacts
        [HttpGet("contacts")]
        public async Task<IActionResult> Contacts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _friendService.GetContactsAsync(userId);
            return list.Success ? Ok(list) : BadRequest(list);
        }

        [HttpGet("mutualContactList")]
        public async Task<IActionResult> getMutualContactList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _friendService.GetMutualContactListAsync(userId);
            return list.Success ? Ok(list) : BadRequest(list);
        }

        // DELETE api/friend/cancel/{requestId}
        [HttpDelete("cancel/{requestId}")]
        public async Task<IActionResult> CancelFriendRequest(Guid requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var resp = await _friendService.CancelRequestAsync(requestId, userId);
            return resp.Success ? Ok(resp) : BadRequest(resp);
        }

         //✅ toggle un/follow friend
        [HttpDelete("toggleUnFollow/{requestId}")]
        public async Task<IActionResult> ToggleFollowRequest(Guid requestId)
        {
            var CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _friendService.ToggleFollowRequestAsync(requestId, CurrentUserId));
        }

        //// ✅ re-follow friend 
        //[HttpPost("unblock/{requestId}")]
        //public async Task<IActionResult> UnBlockFriend(Guid requestId)
        //{
        //    var CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    return Ok(await _friendService.UnBlockUserAsync(requestId, CurrentUserId));
        //}

        // ✅ Check status
        [HttpGet("status")]
        public async Task<IActionResult> CheckStatusOfFriendRequest([FromQuery] string toUserId)
        {
            var CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _friendService.CheckStatusAsync(CurrentUserId, toUserId));
        }
    }

    public record SendFriendRequestBody(string? Message);
    public record RespondBody(bool Accept);

}
