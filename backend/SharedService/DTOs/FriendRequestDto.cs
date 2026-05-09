using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedService.Models;

namespace SharedService.DTOs
{
    public class FriendRequestDto
    {
        public Guid Id { get; set; }

        public string FromUserId { get; set; } = null!;
        public string ToUserId { get; set; } = null!;

        public FriendRequestStatus Status { get; set; } // Or enum if you mapped it as string
        public DateTimeOffset SentAt { get; set; }

        public string? Message { get; set; }

        // Additional user info
        public UserDto? Sender { get; set; }
        public UserDto? Receiver { get; set; }
    }
}
