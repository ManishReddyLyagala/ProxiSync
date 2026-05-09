using ChatService.Dtos;
using SharedService.DTOs;
using SharedService.Models;

namespace ChatService.Interfaces
{
    public interface IChatRepository
    {
        // Conversations
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<Conversation?> GetDirectConversationBetweenAsync(string userA, string userB);
        Task<Conversation> CreateConversationAsync(Conversation conv);
        Task<Response<bool>> DeleteConversationByIdAsync(Guid ConversationId);
        Task<Response<string>> ToggleConversationBlockAsync(Guid conversationId, string userId, bool block);
        Task AddRangeParticipantsAsync(IEnumerable<ConversationParticipant> participants);
        Task RemoveParticipantAsync(Guid conversationId, string requestedBy, string targetUserId);
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 10);
        Task<IEnumerable<ConversationParticipant>> GetParticipantsAsync(Guid conversationId);

        // Messages
        Task<Message> AddMessageAsync(Message msg);
        Task<Message?> GetMessageByIdAsync(Guid messageId);
        Task<MessagePageDto> GetMessagesAsync(Guid conversationId, int page, int pageSize);
        Task<object> CountUnreadWithLastMessageAsync(Guid conversationId, string userId);
        Task<IEnumerable<MessageDto>> GetUnreadForUserAsync(string userId);
        Task<bool> SoftDeleteMessageAsync(Guid messageId, string userId);
        Task<bool> DeleteMessagesUntilAsync(Guid ConversationId);
        Task<MessageDto> EditMessageAsync(string userId, EditMessageDto editDto);
        // Receipts
        Task AddReadReceiptsBulkAsync(List<Guid> messageIds, string userId, Guid conversationId);
        Task<int> CountReadReceiptsForMessageAsync(Guid messageId);
        Task<MessageReadUsersDto?> GetMessageReadUsersAsync(
        Guid conversationId,
        string currentUserId,
        Guid messageId);

    }
}
