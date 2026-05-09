namespace ChatService.Dtos
{
    public class ConversationDto
    {
        public Guid ConversationId { get; set; }
        public string Type { get; set; } = "direct";
        public string? Name { get; set; }
        public bool IsBlocked { get; set; } 
        public string ? BlockedByUserId { get; set; }
        public string? FriendStatus { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public List<ConversationParticipantDto> Participants { get; set; } = new();
        public int UnreadCount { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTimeOffset? LastMessageSentAt { get; set; }
        public string? LastMessageSenderName { get; set; }
    }

    public class ConversationParticipantDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = "member";
        public DateTimeOffset JoinedAt { get; set; }
        public string? DisplayName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool isOnline { get; set; }
        public DateTime lastSeen { get; set; }
    }
}
