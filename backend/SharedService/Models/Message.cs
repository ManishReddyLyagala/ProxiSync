using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
    public class Message
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public string SenderId { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

        // Summary flag — true only when ALL participants have read the message
        public bool IsSeen { get; set; } = false;
        public bool IsEdited { get; set; } = false;

        // Soft delete semantics (simple)
        public bool IsDeleted { get; set; } = false;
        public ICollection<MessageReadReceipt> ReadReceipts { get; set; } = new List<MessageReadReceipt>();
    }
}
