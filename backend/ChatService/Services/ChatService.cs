using ChatService.Dtos;
using ChatService.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedService.Data;
using SharedService.DTOs;
using SharedService.Models;
using System.Security.Cryptography;

namespace ChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _repo;
        private readonly AppDbContext _db;

        public ChatService(IChatRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        // ---------------- Conversations/group ----------------
        public async Task<ConversationDto> CreateConversationAsync(string creatorId, CreateConversationDto dto)
        {
            if (dto.Type == "direct")
            {
                if (dto.ParticipantIds == null || dto.ParticipantIds.Count != 1)
                    throw new ArgumentException("Direct conversation requires exactly one other participant id");

                var other = dto.ParticipantIds.First();

                var isFriend = await _db.FriendRequests.AnyAsync(fr =>
                    (fr.FromUserId == creatorId && fr.ToUserId == other) ||
                    (fr.ToUserId == creatorId && fr.FromUserId == other) &&
                    fr.Status == FriendRequestStatus.Accepted);

                if (!isFriend) throw new Exception("You can only create direct conversations with accepted friends.");

                var existing = await _repo.GetDirectConversationBetweenAsync(creatorId, other);
                if (existing != null) return ToDto(existing, creatorId);

                var conn = new Conversation
                {
                    ConversationId = Guid.NewGuid(),
                    Type = ConversationType.Direct,
                    CreatedBy = creatorId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Participants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant { ConversationId = Guid.Empty, UserId = creatorId, Role = GroupRole.Member, JoinedAt = DateTimeOffset.UtcNow },
                        new ConversationParticipant { ConversationId = Guid.Empty, UserId = other, Role = GroupRole.Member, JoinedAt = DateTimeOffset.UtcNow }
                    }
                };

                foreach (var participant in conn.Participants)
                {
                    participant.ConversationId = conn.ConversationId;
                }

                var savedConv = await _repo.CreateConversationAsync(conn);
                return ToDto(savedConv, creatorId);
            }
            else  // group
            {
                if (string.IsNullOrEmpty(dto.Name)) throw new ArgumentException("Group conversation requires a name");
                var conv = new Conversation
                {
                    ConversationId = Guid.NewGuid(),
                    Type = ConversationType.Group,
                    Name = dto.Name,
                    CreatedBy = creatorId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Participants = new List<ConversationParticipant>()
                };
                conv.Participants.Add(new ConversationParticipant { Id = Guid.NewGuid(), ConversationId = conv.ConversationId, JoinedAt = DateTimeOffset.UtcNow, UserId = creatorId, Role = GroupRole.Admin });

                foreach (var Pid in dto.ParticipantIds.Distinct())
                {
                    if (Pid == creatorId) continue;
                    var isFriend = await _db.FriendRequests.AnyAsync(fr =>
                    (fr.FromUserId == creatorId && fr.ToUserId == Pid) ||
                    (fr.ToUserId == creatorId && fr.FromUserId == Pid) &&
                    fr.Status == FriendRequestStatus.Accepted);

                    if (!isFriend)
                    {
                        throw new InvalidOperationException($"You cannot create a group with this user {Pid}, because you must be friends first.");
                    }
                    conv.Participants.Add(new ConversationParticipant { Id = Guid.NewGuid(), ConversationId = conv.ConversationId, JoinedAt = DateTimeOffset.UtcNow, UserId = Pid, Role = GroupRole.Member });
                }

                var savedConv = await _repo.CreateConversationAsync(conv);
                return ToDto(savedConv, creatorId);
            }
        }

        public async Task<Response<string>> BlockOrUnBlockConversation(Guid conversationId, string userId, bool block)
        {
            var isParticipant = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
            if (!isParticipant) return Response<string>.Fail("Not authorized");
            return await _repo.ToggleConversationBlockAsync(conversationId, userId, block);
        }

        public async Task<ConversationDto?> GetConversationAsync(Guid conversationId, string curUserId)
        {
            var conversation = await _repo.GetConversationByIdAsync(conversationId);
            if (conversation == null) return null;
            return ToDto(conversation, curUserId);
        }

        public async Task<Response<bool>> DeleteConversationAsync(Guid conversationId, string currentUserId)
        {
            var conversation = await _db.Conversations
                .Select(c => new { c.Type, c.ConversationId })
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation == null) return Response<bool>.Fail("Conversation not found.");

            if (conversation.Type == ConversationType.Group)
            {
                var isAdmin = await _db.ConversationParticipants.AnyAsync(p =>
                    p.ConversationId == conversationId &&
                    p.UserId == currentUserId &&
                    (p.Role == GroupRole.Admin || p.Role == GroupRole.Owner));

                if (!isAdmin) return Response<bool>.Fail("Only admins can delete this group.");
            }
            else
            {
                 return Response<bool>.Fail("Direct Conversations Can't be Deleted use 'Block/UnFollow' instead.");
            }

            return await _repo.DeleteConversationByIdAsync(conversationId);
        }
        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 10)
        {
            return await _repo.GetUserConversationsAsync(userId, pageNumber, pageSize);
        }

        public async Task<ConversationDto> GetUserConversationByUserId(string currentUserId, Guid otherUserId)
        {
            var isFriend = await _db.FriendRequests.AnyAsync(fr =>
                   (fr.FromUserId == currentUserId && fr.ToUserId == otherUserId.ToString()) ||
                   (fr.ToUserId == currentUserId && fr.FromUserId == otherUserId.ToString()) &&
                   fr.Status == FriendRequestStatus.Accepted);

            if (!isFriend)
            {
                throw new InvalidOperationException($"invalid operation because you must be friends first.");
            }
            var conversation = await _db.Conversations
        .Include(c => c.Participants)
            .ThenInclude(p => p.User)
        .Where(c => c.Type == ConversationType.Direct)
        // Check that BOTH users are in the participants collection
        .Where(c => c.Participants.Any(p => p.UserId == currentUserId) &&
                    c.Participants.Any(p => p.UserId == otherUserId.ToString()))
        .Select(c => new ConversationDto
        {
            ConversationId = c.ConversationId,
            Type = c.Type.ToString(),
            // Logic to determine the display name (it's the other person's name for direct chats)
            Name = c.Participants.FirstOrDefault(p => p.UserId != currentUserId)!.User.DisplayName,
            IsBlocked = c.IsBlocked,
            CreatedAt = c.CreatedAt,
            CreatedBy = c.CreatedBy
        })
        .FirstOrDefaultAsync();

            if (conversation == null)
            {
                throw new KeyNotFoundException("No existing conversation found between these users.");
            }

            return conversation;
        }

        public async Task AddParticipantsAsync(Guid conversationId, string addingUserId, IEnumerable<AddParticipantDto> participantDtos)
        {
            // 1. Fetch current participants to verify permissions and check for duplicates
            var existingParticipants = await _repo.GetParticipantsAsync(conversationId);

            // 2. Authorization Check (Only Admin/Owner can add)
            var addingUser = existingParticipants.FirstOrDefault(p => p.UserId == addingUserId);
            if (addingUser == null || (addingUser.Role != GroupRole.Admin && addingUser.Role != GroupRole.Owner))
            {
                throw new UnauthorizedAccessException("Not authorized to add participants");
            }

            var newParticipantsList = new List<ConversationParticipant>();
            var existingUserIds = existingParticipants.Select(p => p.UserId).ToHashSet();

            // 3. Process each DTO
            foreach (var dto in participantDtos)
            {
                // Skip if user is already in the conversation
                if (existingUserIds.Contains(dto.UserId)) continue;

                newParticipantsList.Add(new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    UserId = dto.UserId,
                    Role = Enum.TryParse<GroupRole>(dto.Role, true, out var r) ? r : GroupRole.Member,
                    JoinedAt = DateTimeOffset.UtcNow
                });
            }

            // 4. Bulk insert if there are any valid new participants
            if (newParticipantsList.Any())
            {
                await _repo.AddRangeParticipantsAsync(newParticipantsList);
            }
        }
        public async Task RemoveParticipantAsync(Guid conversationId, string removingUserId, string userIdToRemove)
        {
            if (string.IsNullOrEmpty(removingUserId) || string.IsNullOrEmpty(userIdToRemove) || conversationId == Guid.Empty)
            {
                throw new Exception("Invalid Request: User IDs cannot be empty and Conversation ID must be valid.");
            }
            await _repo.RemoveParticipantAsync(conversationId, removingUserId, userIdToRemove);
        }

        // ---------------- Messages ----------------

        public async Task<MessagePageDto> GetMessagesAsync(string userId, Guid conversationId, int page = 1, int pageSize = 50)
        {
            var isParticipant = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
            if (!isParticipant) throw new Exception("Not authorized");

            return await _repo.GetMessagesAsync(conversationId, page, pageSize);
        }

        public async Task<MessageDto> SendMessageAsync(string UserId, SendMessageDto dto)
        {
            var participants = await _db.ConversationParticipants
             .Where(p => p.ConversationId == dto.ConversationId)
             .Select(p => p.UserId)
             .ToListAsync();

            // Ensure current user is actually in this chat
            if (!participants.Contains(UserId))
                throw new Exception("Not Authorized");

            // Find the recipient (Pid)
            var otherPid = participants.FirstOrDefault(id => id != UserId);

            if (string.IsNullOrEmpty(otherPid))
                throw new Exception("Recipient not found in conversation.");

            // 2. Optimized Friend Check
            var isFriend = await _db.FriendRequests.AnyAsync(fr =>
                fr.Status == FriendRequestStatus.Accepted &&
                ((fr.FromUserId == UserId && fr.ToUserId == otherPid) ||
                 (fr.ToUserId == UserId && fr.FromUserId == otherPid)));

            if (!isFriend)
            {
                throw new InvalidOperationException($"Message blocked: You must be friends with {otherPid}.");
            }

            var msg = new Message
            {
                MessageId = Guid.NewGuid(),
                ConversationId = dto.ConversationId,
                SenderId = UserId,
                Content = dto.Content,
                SentAt = DateTimeOffset.UtcNow,
                MessageType = (MessageType)dto.MessageType,
                AttachmentType = dto.AttachmentType,
                AttachmentUrl = dto.AttachmentUrl,
                IsSeen = false
            };

            var saved = await _repo.AddMessageAsync(msg);
            var senderUser = await _db.Users.FindAsync(UserId);

            return new MessageDto
            {
                MessageId = saved.MessageId,
                ConversationId = saved.ConversationId,
                SenderId = saved.SenderId,
                MessageType = (int)saved.MessageType,
                Content = saved.Content,
                AttachmentUrl = saved.AttachmentUrl,
                AttachmentType = saved.AttachmentType,
                SentAt = saved.SentAt,
                IsSeen = saved.IsSeen,
                isEdited = saved.IsEdited,
                SenderDisplayName = senderUser?.DisplayName,
                SenderProfileUrl = senderUser?.ProfilePictureUrl
            };
        }

        public async Task<object> GetUnreadCountAsync(string userId, Guid conversationId)
        {
            var isParticipant = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
            if (!isParticipant) throw new Exception("Not authorized");

            return await _repo.CountUnreadWithLastMessageAsync(conversationId, userId);
        }

        public async Task<MessageReadUsersDto?> GetMessageReadUsersListAsync(string userId, Guid conversationId, Guid messageId)
        {
            var isParticipant = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
            if (!isParticipant) throw new Exception("Not authorized");

            return await _repo.GetMessageReadUsersAsync(conversationId, userId, messageId);
        }

        public async Task<(Guid ConversationId, List<MessageSeenUpdateDto> Updates)> MarkAsReadAsync(string userId, MarkReadDto dto)
        {
            // 1 authorization
            var isParticipant = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == dto.ConversationId && p.UserId == userId);
            if (!isParticipant) throw new Exception("Not authorized");

            // 2️ Load messages up to timestamp (excluding own)
            var messageIds = await _db.Messages
            .Where(m =>
                m.ConversationId == dto.ConversationId &&
                m.SentAt <= dto.UpToTimestamp &&
                m.SenderId != userId)
            .Select(m => m.MessageId)
            .ToListAsync();

            if (!messageIds.Any())
                return (dto.ConversationId, new());

            await _repo.AddReadReceiptsBulkAsync(messageIds, userId, dto.ConversationId);

        var updates = await _db.Messages
         .Where(m => messageIds.Contains(m.MessageId))
         .Select(m => new MessageSeenUpdateDto
         {
             MessageId = m.MessageId,
             IsSeen = m.IsSeen
         })
         .ToListAsync();

            return (dto.ConversationId, updates);
        }

        public async Task<IEnumerable<MessageDto>> GetUnreadMessagesAsync(string userId)
        {
            return await _repo.GetUnreadForUserAsync(userId);
        }

        public async Task<bool> DeleteMessageAsync(string userId, Guid messageId)
        {
           return  await _repo.SoftDeleteMessageAsync(messageId, userId);
        }

        public async Task<bool> ClearAllMessagesUntilAsync(string userId, Guid ConversationId)
        {
            var isParticipant = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == ConversationId && p.UserId == userId);
            if (!isParticipant) return false;
           
            return await _repo.DeleteMessagesUntilAsync(ConversationId);
        }
        public async Task<MessageDto> EditMessageAsync(string userId, EditMessageDto editDto)
        {
            return await _repo.EditMessageAsync(userId, editDto);
        }

        // map Conversation -> DTO
        private ConversationDto ToDto(Conversation conv, string currentUserId)
        {
            //Guid.TryParse(currentUserId, out var curUserId);
            // Find the other participant for Direct Messages
            var otherParticipant = conv.Type == ConversationType.Direct
                ? conv.Participants.FirstOrDefault(p => p.UserId != currentUserId)
                : null;

            // Fetch friend request status if it's a Direct Message
            var friendReq = (conv.Type == ConversationType.Direct && otherParticipant != null)
                ? _db.FriendRequests.FirstOrDefault(f =>
                    (f.FromUserId == currentUserId && f.ToUserId == otherParticipant.UserId) ||
                    (f.ToUserId == currentUserId && f.FromUserId == otherParticipant.UserId))
                : null;

            return new ConversationDto
            {
                ConversationId = conv.ConversationId,
                Type = conv.Type.ToString(),

                // 1. Fixed Name Logic
                Name = conv.Type == ConversationType.Direct
                    ? (otherParticipant?.User?.DisplayName ?? otherParticipant?.User?.UserName)
                    : conv.Name,

                CreatedBy = conv.CreatedBy,
                CreatedAt = conv.CreatedAt,
                IsBlocked = conv.IsBlocked,

                // 2. Logic for BlockedByUserId / ActionByUserId
                // If system blocked is true, use the conv property. 
                // Otherwise, if friend status is "Unfollowed", use ActionByUserId from the request.
                BlockedByUserId = conv.IsBlocked
                    ? conv.BlockedByUserId
                    : (friendReq?.Status == FriendRequestStatus.UnFollowed ? friendReq.ActionByUserId.ToString() : null),

                // 3. Fixed FriendStatus string
                FriendStatus = friendReq?.Status.ToString(),

                Participants = conv.Participants?.Select(p => new ConversationParticipantDto
                {
                    UserId = p.UserId,
                    Role = p.Role.ToString(),
                    JoinedAt = p.JoinedAt,
                    // Assuming p.User navigation property is loaded
                    DisplayName = p.User?.DisplayName,
                    ProfilePictureUrl = p.User?.ProfilePictureUrl
                }).ToList() ?? new List<ConversationParticipantDto>()
            };
        }

        public async Task UpdateUserPresenceAsync(string userId, bool isOnline)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.IsOnline = isOnline;
                user.LastSeen = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetAcceptedFriendIdsAsync(string userId)
        {
            return await _db.FriendRequests
                .Where(fr => (fr.FromUserId == userId || fr.ToUserId == userId) && fr.Status == FriendRequestStatus.Accepted)
                .Select(fr => fr.FromUserId == userId ? fr.ToUserId : fr.FromUserId)
                .Distinct()
                .ToListAsync();
        }
    }
}
