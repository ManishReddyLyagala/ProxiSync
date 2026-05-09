using ChatService.Dtos;
using ChatService.Interfaces;
using ChatService.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SharedService.Models;
using System.Security.Claims;

namespace ChatService.Hubs
{
    [Authorize]
    public class ChatHub: Hub
    {
        private readonly IChatService _chat;
        //private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chat)
        {
            _chat = chat;
            //_logger = logger;
        }
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
               bool isOnline =  PresenceTracker.AddConnection(userId, Context.ConnectionId);

                var friendIds = await _chat.GetAcceptedFriendIdsAsync(userId);
                // NEW: Get everyone currently online and send to the person who just logged in
                var allOnline = PresenceTracker.GetOnlineUserIds();
                var onlineFriends = allOnline.Intersect(friendIds).ToList();
                await Clients.Caller.SendAsync("GetOnlineUsers", onlineFriends);
                if (isOnline)
                {
                    await _chat.UpdateUserPresenceAsync(userId, true);

                    // Notify others (e.g., Everyone or just Friends)
                    await Clients.Users(friendIds).SendAsync("UserPresenceStatusChanged",  new { userId, isOnline = true });
                }

            }
            await base.OnConnectedAsync();

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                bool isOffline = PresenceTracker.RemoveConnection(userId, Context.ConnectionId);
                if (isOffline)
                {
                    var friendIds = await _chat.GetAcceptedFriendIdsAsync(userId);
                    await _chat.UpdateUserPresenceAsync(userId, false);
                    await Clients.Users(friendIds).SendAsync("UserPresenceStatusChanged", new { userId, isOnline = false, lastSeen = DateTime.UtcNow });
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(Guid conversationId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var conv = await _chat.GetConversationAsync(conversationId, userId);
            if (conv == null) throw new HubException("Conversation not found");
            if (!conv.Participants.Any(p => p.UserId == userId))
            {
                throw new HubException("Not a participant");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task LeaveConversation(Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task SendToConversation(SendMessageDto dto)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("Unauthorized");
            var conversation = await _chat.GetConversationAsync(dto.ConversationId, senderId);
            if (conversation != null && conversation.IsBlocked)
            {
                // 2. Notify the sender specifically why it failed
                await Clients.Caller.SendAsync("Error", "Conversation is blocked. Unblock to send messages.");
                return;
            }

            var message = await _chat.SendMessageAsync(senderId, dto);

            await Clients.Group(dto.ConversationId.ToString()).SendAsync("ReceiveMessage", message);
            var participantIds = conversation.Participants.Select(p => p.UserId).ToList();
            await Clients.Users(participantIds).SendAsync("ReceiveMessage", message);
            //// Ack to sender
            //await Clients.Caller.SendAsync("MessageSentAck", message);
        }

        public async Task Typing(Guid conversationId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("Unauthorized");
            await Clients.GroupExcept(conversationId.ToString(), Context.ConnectionId).SendAsync("UserTyping", conversationId, userId);
        }

        public async Task StopTyping(Guid conversationId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("Unauthorized");
            await Clients.GroupExcept(conversationId.ToString(), Context.ConnectionId).SendAsync("UserStoppedTyping", conversationId, userId);
        }

        public async Task MarkRead(MarkReadDto dto)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("Unauthorized");
            var result = await _chat.MarkAsReadAsync(userId, dto);

            await Clients.Group(dto.ConversationId.ToString())
                .SendAsync("MessagesMarkedRead", new
                {
                    conversationId = result.ConversationId,
                    updates = result.Updates
                });
        }

        public async Task EditMessage(EditMessageDto dto)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("Unauthorized");

            var updatedMessage = await _chat.EditMessageAsync(userId, dto);
            if (updatedMessage == null)
                throw new HubException("Edit failed");

            // Notify everyone in conversation
            await Clients.Group(updatedMessage.ConversationId.ToString())
                .SendAsync("MessageEdited", updatedMessage);
        }

    }
}
