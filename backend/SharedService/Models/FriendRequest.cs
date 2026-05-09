using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
   
    public class FriendRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FromUserId { get; set; } = string.Empty;
        public string ToUserId { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey(nameof(FromUserId))]
        public virtual AppUser Sender { get; set; } = null!;

        [ForeignKey(nameof(ToUserId))]
        public virtual AppUser Receiver { get; set; } = null!;
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
        public Guid? ActionByUserId { get; set; }
        public DateTimeOffset SentAt { get; set; } = DateTime.UtcNow;
        public DateTimeOffset? RespondedAt { get; set; }
        public string? Message { get; set; }
    }
}
