using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
    public class ConversationParticipant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
