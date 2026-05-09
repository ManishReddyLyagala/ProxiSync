using SharedService.DTOs;

namespace GatewayAPI.Interfaces
{
    public interface IAuthService
    {
        Task<Response<UserDto>> RegisterAsync(RegisterDto dto);
        Task<Response<UserDto>> RegisterWithImageAsync(RegisterDto dto, IFormFile? image);
        Task<Response<UserDto>> LoginAsync(LoginDto dto);
        Task<Response<bool>> LogoutAsync(string userId);
        Task<Response<UserDto>> RefreshTokenAsync(string token, string refreshToken);
    }

}
