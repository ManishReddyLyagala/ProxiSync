using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using SharedService.Models;

namespace SharedService.Utils
{
    public interface IJwtHelper
    {
        string CreateToken(AppUser user);
        string GenerateRefreshToken(); 
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public class JwtHelper : IJwtHelper
    {
        private readonly IConfiguration _config;
        public JwtHelper(IConfiguration config) => _config = config;

        public string CreateToken(AppUser user)
        {
            var key = _config["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
            var issuer = _config["Jwt:Issuer"] ?? "ProxiSync";
            var audience = _config["Jwt:Audience"] ?? "ProxiSyncClient";
            var expiry = int.TryParse(_config["Jwt:ExpiryInHours"], out var v) ? v : 60;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randonNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randonNumber);
            return Convert.ToBase64String(randonNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                if (!tokenHandler.CanReadToken(token))
                    throw new SecurityTokenException("Invalid token format");

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }
                return principal;
            }
            catch (Exception)
            {
                throw new SecurityTokenException("The provided token is malformed and cannot be decoded.");
            }
        }
}
}
