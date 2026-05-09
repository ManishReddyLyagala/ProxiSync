using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SharedService.Models
{
    public class AppUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
        public string? Bio { get; set; }

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
