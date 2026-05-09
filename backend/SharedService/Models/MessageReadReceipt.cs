using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
    public class MessageReadReceipt
    {
        // Composite key (MessageId, UserId) — configured in OnModelCreating
        public Guid MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
