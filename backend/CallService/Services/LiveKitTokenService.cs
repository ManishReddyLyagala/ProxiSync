using CallService.Config;
using CallService.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CallService.Services
{
    public class LiveKitTokenService : ILiveKitTokenService
    {
        private readonly LiveKitSettings _settings;

        public LiveKitTokenService(IOptions<LiveKitSettings> settings)
        {
            _settings = settings.Value;
        }

        public string CreateToken(string roomName, string userId, string userName)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.ApiSecret))
                throw new Exception("LiveKit settings missing (ApiKey/ApiSecret)");

            var now = DateTimeOffset.UtcNow;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.ApiSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // IMPORTANT: video must be OBJECT not string
            var payload = new JwtPayload
            {
                { "iss", _settings.ApiKey },
                { "sub", userId },
                { "name", userName },
                { "nbf", now.ToUnixTimeSeconds() },
                { "exp", now.AddHours(2).ToUnixTimeSeconds() },

                // LiveKit expects this as JSON object
                { "video", new Dictionary<string, object>
                    {
                        { "room", roomName },
                        { "roomJoin", true },
                        { "canPublish", true },
                        { "canSubscribe", true },
                        { "canPublishData", true }
                    }
                }
            };

            var token = new JwtSecurityToken(
                header: new JwtHeader(creds),
                payload: payload
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GetLiveKitUrl() => _settings.Url;
    }
}
