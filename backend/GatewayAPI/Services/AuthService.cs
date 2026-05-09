using GatewayAPI.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SharedService.Data;
using SharedService.DTOs;
using SharedService.Models;
using SharedService.Services;

namespace GatewayAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IFileService _fileService;
        private readonly ITokenService _tokenService;

        public AuthService(
            UserManager<AppUser> userManager,
            IFileService fileService,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _fileService = fileService;
            _tokenService = tokenService;
        }

        public async Task<Response<UserDto>> RegisterAsync(RegisterDto dto)
        {
            return await RegisterWithImageAsync(dto, dto.ProfileImage);
        }

        public async Task<Response<UserDto>> RegisterWithImageAsync(RegisterDto dto, IFormFile? image)
        {
            // Username Check
            if (await _userManager.FindByNameAsync(dto.Username) != null)
                return Response<UserDto>.Fail("Username already exists.");

            // Email Check
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return Response<UserDto>.Fail("Email already registered.");

            // Create User
            var user = new AppUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                DisplayName = dto.DisplayName,
                CreatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                IsOnline = false,
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
                return Response<UserDto>.Fail(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            // Profile Image Upload
            if (image != null)
            {
                var imgUrl = await _fileService.UploadProfileImageAsync(image);
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    user.ProfilePictureUrl = imgUrl;
                    await _userManager.UpdateAsync(user);
                }
            }

            // Generate Token
            var token = _tokenService.CreateToken(user);

            // Prepare DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsOnline = user.IsOnline
            };

            return Response<UserDto>.Ok(userDto, "Registered successfully");
        }

        public async Task<Response<UserDto>> LoginAsync(LoginDto dto)
        {
            AppUser? user = await _userManager.FindByNameAsync(dto.UsernameOrEmail)
                           ?? await _userManager.FindByEmailAsync(dto.UsernameOrEmail);

            if (user == null) return Response<UserDto>.Fail("Invalid credentials.");

            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword) return Response<UserDto>.Fail("Invalid credentials.");

            var token = _tokenService.CreateToken(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                DisplayName = user.DisplayName,
                Token = token,
                IsDeleted = user.IsDeleted,
                IsOnline = user.IsOnline,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio
            };

            if (user.IsDeleted)
            {
                return Response<UserDto>.Ok(userDto, "Account is deactivated. Restoration required.");
            }

            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(2);
            user.IsOnline = true;
            await _userManager.UpdateAsync(user);

            userDto.RefreshToken = refreshToken;
            userDto.IsOnline = true;

            return Response<UserDto>.Ok(userDto, "Login successful");
        }

        public async Task<Response<bool>> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Response<bool>.Fail("User not found.");

            user.IsOnline = false;
            user.LastSeen = DateTime.UtcNow;
            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);

            return Response<bool>.Ok(true, "Logout successful");
        }

        public async Task<Response<UserDto>> RefreshTokenAsync(string token, string refreshToken)
        {
            try
            {
                var principal = _tokenService.GetPrincipalFromExpiredToken(token);
                var username = principal.Identity?.Name;
                var user = await _userManager.FindByNameAsync(username!);
                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                    return Response<UserDto>.Fail("Invalid refresh token request");

                var newAccessToken = _tokenService.CreateToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                await _userManager.UpdateAsync(user);
                return Response<UserDto>.Ok(new UserDto
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (SecurityTokenException)
            {
                return Response<UserDto>.Fail("Invalid token provided for refresh.");
            }
            catch (Exception e)
            {
                return Response<UserDto>.Fail("Backend failed "+ e.Message);
            }
        }
    }
}
