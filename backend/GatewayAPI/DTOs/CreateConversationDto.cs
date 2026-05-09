namespace GatewayAPI.DTOs
{
    public class CreateConversationDto
    {
        public string Type { get; set; } = "direct"; // "direct" | "group"
        public string? Name { get; set; } // required for group
        public List<string> ParticipantIds { get; set; } = new();
    }
}
