namespace ChatService.Dtos
{
    public class AddParticipantDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = "member";
    }
}
