using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
    public class CallParticipant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CallId { get; set; }
        public CallSession CallSession { get; set; } = null!;
        // status
        public ParticiapantCallStatus Status { get; set; } = ParticiapantCallStatus.Invited;
        // invited, joined, left, missed, rejected

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;

        // initial settings
        public bool IsMicEnabled { get; set; } = false;
        public bool IsVideoEnabled { get; set; } = false;
        public DateTimeOffset InvitedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool Joined { get; set; } = false;
        public DateTimeOffset? JoinedAt { get; set; }

        public bool Left { get; set; } = false;
        public DateTimeOffset? LeftAt { get; set; }
    }
}
