using ChatService.Dtos;
using SharedService.DTOs;
using SharedService.Models;

namespace ChatService.Interfaces
{
    public interface IChatService
    {
        // Conversations
        Task<ConversationDto> CreateConversationAsync(string creatorId, CreateConversationDto dto);
        Task<Response<string>> BlockOrUnBlockConversation(Guid conversationId, string userId, bool block);
        Task<ConversationDto?> GetConversationAsync(Guid conversationId, string curUserId);
        Task<Response<bool>> DeleteConversationAsync(Guid ConversationId, string currentUserId);
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 10);
        Task<ConversationDto> GetUserConversationByUserId(string currentUserId, Guid otherUserId);
        Task AddParticipantsAsync(Guid conversationId, string addingUserId, IEnumerable<AddParticipantDto> participantDtos);
        Task RemoveParticipantAsync(Guid conversationId, string actorUserId, string userIdToRemove);

        // Messages
        Task<MessageDto> SendMessageAsync(string senderId, SendMessageDto dto);
        Task<MessagePageDto> GetMessagesAsync(string userId, Guid conversationId, int page = 1, int pageSize = 50);
        Task<object> GetUnreadCountAsync(string userId, Guid conversationId);
        Task<(Guid ConversationId, List<MessageSeenUpdateDto> Updates)> MarkAsReadAsync(string userId, MarkReadDto dto);
        Task<IEnumerable<MessageDto>> GetUnreadMessagesAsync(string userId);
        Task<MessageReadUsersDto?> GetMessageReadUsersListAsync(string userId, Guid conversationId, Guid messageId);
        Task<bool> DeleteMessageAsync(string userId, Guid messageId);

        Task<bool> ClearAllMessagesUntilAsync(string userId, Guid ConversationId);

        Task<MessageDto> EditMessageAsync(string userId, EditMessageDto editDto);
        Task UpdateUserPresenceAsync(string userId, bool isOnline);
        Task<List<string>> GetAcceptedFriendIdsAsync(string userId);
    }
}
