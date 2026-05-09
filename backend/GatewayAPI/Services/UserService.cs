using GatewayAPI.DTOs;
using GatewayAPI.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedService.Data;
using SharedService.DTOs;
using SharedService.Models;

namespace GatewayAPI.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _um;
        private readonly AppDbContext _db;
        private readonly IFileService _fileService;

        public UserService(UserManager<AppUser> um, AppDbContext db, IFileService fileService)
        {
            _um = um;
            _db = db;
            _fileService = fileService;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return _um.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                DisplayName = u.DisplayName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsOnline = u.IsOnline
            }).ToList();
        }

        // ✅ Get all users except self
        public async Task<Response<IEnumerable<UserDto>>> GetAllUsersExceptSelfAsync(string currentUserId)
        {
            var users = await _um.Users
                .Where(u => u.Id != currentUserId && !(_db.FriendRequests.Any(fr =>
                    (fr.FromUserId == currentUserId && fr.ToUserId == u.Id) ||
                    (fr.ToUserId == currentUserId && fr.FromUserId == u.Id)))
                )
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.UserName ?? string.Empty,
                    DisplayName = u.DisplayName,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    IsOnline = u.IsOnline
                })
                .ToListAsync();

            return Response<IEnumerable<UserDto>>.Ok(users, "Users fetched successfully");
        }

        public async Task<UserDetailDto?> GetByIdAsync(string id)
        {
            var u = await _um.FindByIdAsync(id);
            if (u == null) return null;
            return new UserDetailDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                DisplayName = u.DisplayName,
                Email = u.Email!,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsOnline = u.IsOnline,
                Bio = u.Bio,
                LastSeen = u.LastSeen,
                CreatedAt = u.CreatedAt
            };
        }

        // ✅ Search users (multiple results)
        public async Task<Response<List<UserDto>>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Response<List<UserDto>>.Fail("Search query required");

            var users = await _um.Users
                .Where(u => u.UserName!.Contains(query) ||
                            (u.DisplayName != null && u.DisplayName.Contains(query)))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.UserName ?? string.Empty,
                    DisplayName = u.DisplayName,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    IsOnline = u.IsOnline
                })
                .ToListAsync();

            return Response<List<UserDto>>.Ok(users);
        }

        public async Task<Response<UserDto>> UpdateAsync(string id, UpdateUserDto dto)
        {
            var u = await _um.FindByIdAsync(id);
            if (u == null) return Response<UserDto>.Fail("User not found.");

            if (dto.ProfileImage != null)
            {
                // 1. Delete existing image if it exists
                if (!string.IsNullOrEmpty(u.ProfilePictureUrl))
                {
                    _fileService.DeleteImage(u.ProfilePictureUrl);
                }

                // 2. Upload new image
                u.ProfilePictureUrl = await _fileService.UploadProfileImageAsync(dto.ProfileImage);
            }
            u.DisplayName = dto.DisplayName ?? u.DisplayName;
            u.Bio = dto.Bio;

            var res = await _um.UpdateAsync(u);
            if (!res.Succeeded) return Response<UserDto>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));

            var updated = new UserDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                DisplayName = u.DisplayName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsOnline = u.IsOnline,
                Bio = u.Bio
            };

            return Response<UserDto>.Ok(updated, "User profile updated successfully");
        }

        public async Task<bool> SoftDeleteAccountAsync(string userId)
        {
            var user = await _um.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.RefreshToken = null; // Force logout everywhere

            await _um.UpdateAsync(user);
            return true;
        }

        public async Task<bool> RestoreAccountAsync(string userId)
        {
            var user = await _um.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsDeleted = false;
            user.DeletedAt = null;

            await _um.UpdateAsync(user);
            return true;
        }
        //public async Task<Response<string>> DeleteAsync(string id)
        //{
        //    var u = await _um.FindByIdAsync(id);
        //    if (u == null) return Response<string>.Fail("User not found.");

        //    var res = await _um.DeleteAsync(u);
        //    if (!res.Succeeded) return Response<string>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));

        //    return Response<string>.Ok("User deleted", "Account deleted successfully");
        //}
    }
}
