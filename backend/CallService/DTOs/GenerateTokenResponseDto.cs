namespace CallService.DTOs
{
    public class GenerateTokenResponseDto
    {
        public Guid CallId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string LiveKitUrl { get; set; } = string.Empty;
    }
}
