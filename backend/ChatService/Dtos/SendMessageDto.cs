namespace ChatService.Dtos
{
    public class SendMessageDto
    {
        public Guid ConversationId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
        public int MessageType { get; set; } = (int)SharedService.Models.MessageType.Text;
    }
}
