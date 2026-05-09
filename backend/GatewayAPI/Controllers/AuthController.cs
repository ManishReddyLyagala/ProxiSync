using GatewayAPI.Interfaces;
using GatewayAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedService.DTOs;
using System.Security.Claims;

namespace GatewayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        //[DisableRequestSizeLimit]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
        {
            var resp = await _auth.RegisterAsync(dto);
            if (!resp.Success) return BadRequest(resp);
            return Ok(resp);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var resp = await _auth.LoginAsync(dto);
            if (!resp.Success) return Unauthorized(resp);
            return Ok(resp);
        }

        //[Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            if (string.IsNullOrEmpty(userId)) return Unauthorized(Response<bool>.Fail("Invalid user"));

            var resp = await _auth.LogoutAsync(userId);

            if (!resp.Success) return BadRequest(resp);
            return Ok(resp);
        }

        [HttpPost("refreshtoken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Invalid client request");
            }

            var result = await _auth.RefreshTokenAsync(request.Token, request.RefreshToken);

            if (!result.Success)
            {
                return Unauthorized(result); // Returns 401 if refresh token is expired or invalid
            }

            return Ok(result);
        }
    }
}
