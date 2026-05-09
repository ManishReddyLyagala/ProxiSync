namespace ChatService.Dtos
{
    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public int MessageType { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
        public DateTimeOffset SentAt { get; set; }
        public bool IsSeen { get; set; }
        public bool isEdited { get; set; }
        public string? SenderDisplayName { get; set; }
        public string? SenderProfileUrl { get; set; }
    }
}
