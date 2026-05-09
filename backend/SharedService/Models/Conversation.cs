using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
    public class Conversation
    {
        public Guid ConversationId { get; set; } = Guid.NewGuid();
        public ConversationType Type { get; set; } = ConversationType.Direct;
        public string? Name { get; set; } // group name only // todo all names even direct also.

        public DateTimeOffset? DeletedUntil { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockedByUserId { get; set; } // The ID of the person who initiated the block
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
