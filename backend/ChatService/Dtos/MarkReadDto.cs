namespace ChatService.Dtos
{
    public class MarkReadDto
    {
        public Guid ConversationId { get; set; }
        public DateTimeOffset UpToTimestamp { get; set; } // mark messages with SentAt <= this as read
    }
    public class MessageSeenUpdateDto
    {
        public Guid MessageId { get; set; }
        public bool IsSeen { get; set; }
    }

    public class SeenUsersDetailDto
    {
        public string? UserId { get; set;} 
        public string UserName { get; set;} = null!;
        public string? DisplayName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTimeOffset ReadAt { get; set; }
       
    }
    public class MessageReadUsersDto
    {
        public Guid ConversationId { get; set; }
        public Guid MessageId { get; set; }
        public List<SeenUsersDetailDto> SeenUsers { get; set; } = new List<SeenUsersDetailDto>();
    }

}
