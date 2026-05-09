using ChatService.Dtos;
using ChatService.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedService.Data;
using SharedService.DTOs;
using SharedService.Models;
using System.Linq.Expressions;

namespace ChatService.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _db;

        public ChatRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
        {
            return await _db.Conversations
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .AsNoTracking() // improves speed and saves memory
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task<Conversation?> GetDirectConversationBetweenAsync(string userA, string userB)
        {
            return await _db.Conversations
                .Where(c => c.Type == ConversationType.Direct)
                .Include(c => c.Participants)
                .Where(c => c.Participants.Count == 2 &&
                            c.Participants.Any(p => p.UserId == userA) &&
                            c.Participants.Any(p => p.UserId == userB))
                .FirstOrDefaultAsync();
        }

        public async Task<Conversation> CreateConversationAsync(Conversation conv)
        {
            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync();
            return conv;
        }

        public async Task<Response<bool>> DeleteConversationByIdAsync(Guid conversationId)
        {
            var rowsAffected = await _db.Conversations
                .Where(c => c.ConversationId == conversationId)
                .ExecuteDeleteAsync();

            return rowsAffected > 0
                ? Response<bool>.Ok(true, "Conversation deleted.")
                : Response<bool>.Fail("Delete failed or conversation not found.");
        }
        public async Task AddRangeParticipantsAsync(IEnumerable<ConversationParticipant> participants)
        {
            await _db.ConversationParticipants.AddRangeAsync(participants);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveParticipantAsync(Guid conversationId, string requestedBy, string targetUserId)
        {
            var conversation = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation == null) throw new Exception("Conversation not found");

            var participants = conversation.Participants.ToList();
            var actor = participants.FirstOrDefault(p => p.UserId == requestedBy);
            var target = participants.FirstOrDefault(p => p.UserId == targetUserId);

            if (target == null) throw new Exception("User is not in this group");

            bool isSelfExit = requestedBy == targetUserId;

            // 1. Authorization Check
            if (!isSelfExit)
            {
                if (actor == null || (actor.Role != GroupRole.Admin && actor.Role != GroupRole.Owner))
                    throw new Exception("Only admins can remove participants");

                if (target.Role == GroupRole.Owner)
                    throw new Exception("The Owner cannot be removed. They must transfer ownership or delete the group.");
            }

            // 2. Group Ownership/Admin Transfer (If leaving user was an Admin/Owner)
            if (isSelfExit && (target.Role == GroupRole.Admin || target.Role == GroupRole.Owner))
            {
                var otherParticipants = participants.Where(p => p.UserId != targetUserId).OrderBy(p => p.JoinedAt).ToList();

                if (otherParticipants.Any())
                {
                    // If no other admin exists, promote the oldest member to Admin
                    if (!otherParticipants.Any(p => p.Role == GroupRole.Admin || p.Role == GroupRole.Owner))
                    {
                        otherParticipants.First().Role = GroupRole.Admin;
                    }
                }
            }

            // 3. Blocking State Cleanup
            // If the person leaving was the one who triggered a block, reset the group state
            if (conversation.IsBlocked && conversation.BlockedByUserId == targetUserId)
            {
                conversation.IsBlocked = false;
                conversation.BlockedByUserId = null;
            }

            // 4. Perform Removal
            _db.ConversationParticipants.Remove(target);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;

            var baseQuery = await _db.Conversations
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .Select(c => new
                {
                    Conv = c,
                    Participants = c.Participants
                    .Select(p => new
                    {
                        p.UserId,
                        p.Role,
                        p.JoinedAt,
                        p.User.DisplayName,
                        p.User.ProfilePictureUrl,
                        p.User.IsOnline,
                        p.User.IsDeleted,
                        p.User.LastSeen
                    })
                    .ToList(),

                   OtherUserId = c.Type == ConversationType.Direct
                    ? c.Participants
                        .Where(p => p.UserId != userId)
                        .Select(p => p.UserId)
                        .FirstOrDefault()
                    : null,

                    UnreadCount = c.Messages.Count(m =>
                        m.SenderId != userId &&
                        !m.IsSeen &&
                        !m.IsDeleted &&
                        (c.DeletedUntil == null || m.SentAt > c.DeletedUntil)),

                    LastMsg = c.Messages
                        .Where(m => !m.IsDeleted && (c.DeletedUntil == null || m.SentAt > c.DeletedUntil))
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.LastMsg != null ? x.LastMsg.SentAt : x.Conv.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();  

                var otherUserIds = baseQuery
                .Where(x => x.OtherUserId != null)
                .Select(x => x.OtherUserId!)
                .ToList();

            var friendRequests = await _db.FriendRequests
                .Where(fr =>
                    (fr.FromUserId == userId && otherUserIds.Contains(fr.ToUserId)) ||
                    (fr.ToUserId == userId && otherUserIds.Contains(fr.FromUserId)))
                .ToListAsync();

            return baseQuery
        .Where(x => x.Conv.Type != ConversationType.Direct ||
                friendRequests.Any(fr =>
                    ((fr.FromUserId == userId && fr.ToUserId == x.OtherUserId) ||
                        (fr.ToUserId == userId && fr.FromUserId == x.OtherUserId)) &&
                    (fr.Status == FriendRequestStatus.Accepted || fr.Status == FriendRequestStatus.UnFollowed)))
        .Select(x =>
        {
        var fr = friendRequests.FirstOrDefault(f =>
            (f.FromUserId == userId && f.ToUserId == x.OtherUserId) ||
            (f.ToUserId == userId && f.FromUserId == x.OtherUserId));

            return new ConversationDto
            {
                ConversationId = x.Conv.ConversationId,
                Type = x.Conv.Type.ToString(),
                CreatedAt = x.Conv.CreatedAt,
                CreatedBy = x.Conv.CreatedBy,
                IsBlocked = x.Conv.IsBlocked,
                BlockedByUserId = x.Conv.IsBlocked
                        ? x.Conv.BlockedByUserId
                        : (fr?.Status == FriendRequestStatus.UnFollowed ? fr.ActionByUserId.ToString() : null),
                FriendStatus = fr?.Status.ToString(),

                Name = x.Conv.Type == ConversationType.Direct
                    ? x.Participants.FirstOrDefault(p => p.UserId != userId)?.DisplayName
                    : x.Conv.Name,

                UnreadCount = x.UnreadCount,
                IsDeleted = x.Conv.Type == ConversationType.Direct &&
                x.Participants.Any(p => p.UserId != userId && p.IsDeleted),
                LastMessageContent = x.LastMsg?.Content,
                LastMessageSentAt = x.LastMsg?.SentAt,
                LastMessageSenderName = x.LastMsg != null
                    ? x.Participants
                        .Where(p => p.UserId == x.LastMsg.SenderId)
                        .Select(p => p.DisplayName)
                        .FirstOrDefault()
                    : null,

                Participants = x.Participants.Select(p => new ConversationParticipantDto
                {
                    UserId = p.UserId,
                    Role = p.Role.ToString(),
                    JoinedAt = p.JoinedAt,
                    DisplayName = p.DisplayName,
                    ProfilePictureUrl = p.ProfilePictureUrl
                }).ToList()
            };
        }).ToList();
        }

        //TODO: The Issue: if The BlockedByUserId is now someone who isn't even in the group anymore. then we can't unblock the group.

        // The Fix: In your RemoveParticipant logic, add a check: If the user being removed is the BlockedByUserId, reset IsBlocked to false or transfer block ownership to the Creator.
        public async Task<Response<string>> ToggleConversationBlockAsync(Guid conversationId, string userId, bool block)
        {
            var conv = await _db.Conversations.Include(c => c.Participants)
                                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conv == null) return Response<string>.Fail("Conversation not found");

            // Authorization Logic
            if (conv.Type == ConversationType.Group)
            {
                // Check if the user is either the Creator OR has an Admin role
                bool isAuthorized = conv.CreatedBy == userId ||
                                    conv.Participants.Any(p => p.UserId == userId && p.Role == GroupRole.Admin);
                if (!isAuthorized)
                    return Response<string>.Fail("Only admins or the creator can block group chats");
            }
            else
            {
                // For 1-on-1, just verify the user is actually part of this conversation
                if (!conv.Participants.Any(p => p.UserId == userId))
                    return Response<string>.Fail("You are not a participant in this conversation");
            }

            if (block)
            {
                if (conv.IsBlocked && conv.BlockedByUserId != userId)
                    return Response<string>.Fail($"This conversation is already blocked by another user.");

                conv.IsBlocked = true;
                conv.BlockedByUserId = userId;
            }
            else
            {
                // Only the person who initiated the block can undo it.
                if (conv.BlockedByUserId != userId)
                    return Response<string>.Fail("Only the user who blocked this conversation can unblock it");

                conv.IsBlocked = false;
                conv.BlockedByUserId = null;
            }

            await _db.SaveChangesAsync();
            return Response<string>.Ok(null, block ? "Blocked" : "Unblocked");
        }

        public async Task<IEnumerable<ConversationParticipant>> GetParticipantsAsync(Guid conversationId)
        {
            return await _db.ConversationParticipants.Where(p => p.ConversationId == conversationId).ToListAsync();
        }

        // Messages
        public async Task<Message> AddMessageAsync(Message msg)
        {
            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();
            return msg;
        }

        public async Task<MessagePageDto> GetMessagesAsync(Guid conversationId, int pageNumber, int pageSize)
        {
            var deleteUntilDate = await _db.Conversations.Where(c => c.ConversationId == conversationId).Select(c => c.DeletedUntil).FirstOrDefaultAsync();

            var query = _db.Messages.Where(m => m.ConversationId == conversationId && !m.IsDeleted);
            if (deleteUntilDate.HasValue)
            {
                query = query.Where(m => m.SentAt > deleteUntilDate.Value);
            }

            var messages = query.OrderByDescending(m => m.SentAt);
            var totalCount = await messages.CountAsync();
            var pagedMessages = await messages.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var userids = pagedMessages.Select(m => m.SenderId).Distinct();
            var users = await _db.Users.Where(u => userids.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            var messageDtos = pagedMessages.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                MessageType = (int)m.MessageType,
                Content = m.Content,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentType = m.AttachmentType,
                SentAt = m.SentAt,
                IsSeen = m.IsSeen,
                isEdited = m.IsEdited,
                SenderDisplayName = users.ContainsKey(m.SenderId) ? users[m.SenderId].DisplayName : null,
                SenderProfileUrl = users.ContainsKey(m.SenderId) ? users[m.SenderId].ProfilePictureUrl : null
            }).ToList();

            return new MessagePageDto { Messages = messageDtos, TotalCount = totalCount };
        }

        public async Task<Message?> GetMessageByIdAsync(Guid messageId)
        {
            return await _db.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<object> CountUnreadWithLastMessageAsync(Guid conversationId, string userId)
        {
            var query = _db.Messages
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsSeen);

            var unreadCount = await query.CountAsync();
            var lastMessage = await query.OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();

            return new
            {
                UnreadCount = unreadCount,
                LastMessageContent = lastMessage?.Content
            };
        }

        public async Task<MessageReadUsersDto?> GetMessageReadUsersAsync(
         Guid conversationId,
         string currentUserId,
         Guid messageId)
        {
            // 1️⃣ Validate message exists & belongs to conversation
            var message = await _db.Messages
                .Where(m =>
                    m.MessageId == messageId &&
                    m.ConversationId == conversationId)
                .Select(m => new
                {
                    m.MessageId,
                    m.SenderId
                })
                .FirstOrDefaultAsync();

            if (message == null)
                return null;

            // 2️⃣ Only sender can see "seen by" (optional but recommended)
            if (message.SenderId != currentUserId)
                throw new UnauthorizedAccessException("Only sender can view read users");

            // 3️⃣ Get users who read this message
            var seenUsers = await _db.MessageReadReceipts
                .Where(r => r.MessageId == messageId)
                .Include(r => r.User)
                .Select(r =>  new SeenUsersDetailDto
                    {
                        UserId = r.UserId,
                        UserName = r.User.UserName!,
                        DisplayName = r.User.DisplayName,
                        ProfilePictureUrl = r.User.ProfilePictureUrl,
                        IsOnline = r.User.IsOnline,
                        ReadAt = r.ReadAt
                 })
                .ToListAsync();

            // 4️⃣ Return DTO
            return new MessageReadUsersDto
            {
                ConversationId = conversationId,
                MessageId = messageId,
                SeenUsers = seenUsers
            };
        }


        public async Task<IEnumerable<MessageDto>> GetUnreadForUserAsync(string userId)
        {
            var items = await _db.Messages
                .Where(m => m.SenderId != userId && !m.IsSeen)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var userIds = items.Select(i => i.SenderId).Distinct().ToList();
            var users = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            return items.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                MessageType = (int)m.MessageType,
                Content = m.Content,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentType = m.AttachmentType,
                SentAt = m.SentAt,
                IsSeen = m.IsSeen,
                SenderDisplayName = users.ContainsKey(m.SenderId) ? users[m.SenderId].DisplayName : null,
                SenderProfileUrl = users.ContainsKey(m.SenderId) ? users[m.SenderId].ProfilePictureUrl : null
            }).ToList();
        }

        public async Task<bool> SoftDeleteMessageAsync(Guid messageId, string userId)
        {
            try
            {
                var m = await _db.Messages.FindAsync(messageId);
                if (m == null) return false;

                if(m.SenderId != userId) return false;
                // For group we don't implement per-user delete table here to keep it simple
                m.IsDeleted = true;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // clear all
        public async Task<bool> DeleteMessagesUntilAsync(Guid ConversationId)
        {
            try
            {
                var conv = await _db.Conversations.FindAsync(ConversationId);
                if(conv == null) return false;
                conv.DeletedUntil = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<MessageDto?> EditMessageAsync(string userId, EditMessageDto dto)
        {
            var messageId = Guid.Parse(dto.MessageId);

            var message = await _db.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);

            if (message == null) return null;
            if(message.Conversation.IsBlocked) return null;
            if (message.SenderId != userId) return null;

            message.Content = dto.NewContent;
            message.IsEdited = true;

            await _db.SaveChangesAsync();

            return new MessageDto
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                MessageType = (int)message.MessageType,
                Content = message.Content,
                AttachmentUrl = message.AttachmentUrl,
                AttachmentType = message.AttachmentType,
                SentAt = message.SentAt,
                IsSeen = message.IsSeen,
                isEdited = message.IsEdited
            };
        }

        // Receipts
        public async Task AddReadReceiptsBulkAsync(List<Guid> messageIds, string userId, Guid conversationId)
        {
            // 3️ Existing receipts for this user
            var existingReceipts = await _db.MessageReadReceipts
                .Where(r => messageIds.Contains(r.MessageId) && r.UserId == userId)
                .Select(r => r.MessageId)
                .ToListAsync();

            // 4️ New receipts only
            var newReceipts = messageIds
                .Except(existingReceipts)
                .Select(messageId => new MessageReadReceipt
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = DateTimeOffset.UtcNow
                })
                .ToList();

            if (!newReceipts.Any()) return;

            await _db.MessageReadReceipts.AddRangeAsync(newReceipts);
            await _db.SaveChangesAsync();

            // 5️ Optional optimization: mark IsSeen only if all read (GROUP SAFE)
            var participantCount = await _db.ConversationParticipants
                .CountAsync(p => p.ConversationId == conversationId);

          var fullyReadIds = await _db.MessageReadReceipts
            .Where(r => messageIds.Contains(r.MessageId))
            .GroupBy(r => r.MessageId)
            .Where(g => g.Count() >= participantCount - 1) // exclude sender
            .Select(g => g.Key)
            .ToListAsync();

            if (fullyReadIds.Any())
            {
                await _db.Messages
                    .Where(m => fullyReadIds.Contains(m.MessageId))
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(m => m.IsSeen, true));
            }
        }


        public async Task<int> CountReadReceiptsForMessageAsync(Guid messageId)
        {
            return await _db.MessageReadReceipts.CountAsync(r => r.MessageId == messageId);
        }
    }
}
