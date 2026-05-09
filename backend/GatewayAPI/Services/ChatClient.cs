using GatewayAPI.DTOs;
using GatewayAPI.Interfaces;
using System.Net.Http.Headers;

namespace GatewayAPI.Services
{
    public class ChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ChatClient> _logger;
        public ChatClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<ChatClient> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<bool> CreateConversationAsync(CreateConversationDto dto)
        {
            try
            {
                var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                if (!string.IsNullOrEmpty(authHeader))
                {
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }

                var response = await _httpClient.PostAsJsonAsync("api/conversations", dto);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("ChatService returned error: {Error}", error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call ChatService CreateConversation");
                return false;
            }
        }
    }
}
