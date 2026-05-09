using GatewayAPI.DTOs;

namespace GatewayAPI.Interfaces
{
    public interface IChatClient
    {
        Task<bool> CreateConversationAsync(CreateConversationDto dto);
    }
}
