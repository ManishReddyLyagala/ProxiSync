namespace GatewayAPI.DTOs
{
    public class UserDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }

        public string? Bio { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
