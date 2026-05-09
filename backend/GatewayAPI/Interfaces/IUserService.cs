using GatewayAPI.DTOs;
using SharedService.DTOs;

namespace GatewayAPI.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDetailDto?> GetByIdAsync(string id);

        Task<Response<IEnumerable<UserDto>>> GetAllUsersExceptSelfAsync(string currentUserId);
        Task<Response<UserDto>> UpdateAsync(string id, UpdateUserDto dto);
        Task<bool> SoftDeleteAccountAsync(string userId);
        Task<bool> RestoreAccountAsync(string userId);
        Task<Response<List<UserDto>>> SearchUsersAsync(string query);
    }
}
