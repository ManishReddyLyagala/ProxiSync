using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
        public string? Bio { get; set; }
        public bool IsDeleted { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
