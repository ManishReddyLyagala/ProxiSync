using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
    public class CallSession
    {
        [Key]
        public Guid CallId { get; set; } = Guid.NewGuid();

        public Guid ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public Conversation Conversation { get; set; } = null!;

        // RoomName used in LiveKit
        [MaxLength(150)]
        public string RoomName { get; set; } = string.Empty;

        public CallType Type { get; set; } = CallType.Audio;
        public CallStatus Status { get; set; } = CallStatus.Ringing;

        public bool IsGroupCall { get; set; }
        public string? LiveKitSid { get; set; }
        public string StartedByUserId { get; set; } = string.Empty;

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndedAt { get; set; }
        public bool HasAnyReceiverJoined { get; set; }

        // Navigation
        public List<CallParticipant> Participants { get; set; } = new();
    }
}
