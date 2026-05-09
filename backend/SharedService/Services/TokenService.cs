using Microsoft.Extensions.Configuration;
using SharedService.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedService.Models;
using SharedService.Services;
using System.Security.Claims;

namespace SharedService.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtHelper _jwt;
        public TokenService(IConfiguration config) => _jwt = new JwtHelper(config);
        public string CreateToken(AppUser user) => _jwt.CreateToken(user);
        public string GenerateRefreshToken() => _jwt.GenerateRefreshToken();
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token) => _jwt.GetPrincipalFromExpiredToken(token);
    }
}
