using GatewayAPI.Interfaces;
using GatewayAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedService.DTOs;

namespace GatewayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _users;
        public UserController(IUserService users) => _users = users;

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _users.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> All() => Ok(await _users.GetAllAsync());

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _users.GetAllUsersExceptSelfAsync(CurrentUserId!);
            return Ok(result);
        }

        // ✅ Search users
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var result = await _users.SearchUsersAsync(q);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserDto dto)
        {
            var CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _users.UpdateAsync(CurrentUserId!, dto);
            return Ok(result);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteAccount()
        {
            var CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _users.SoftDeleteAccountAsync(CurrentUserId!);
            return Ok(result);
        }

        [HttpPost("restoreaccount")]
        public async Task<IActionResult> RestoreAccount()
        {
            var CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _users.RestoreAccountAsync(CurrentUserId!);
            return Ok(result);
        }
    }
}
