using SharedService.Models;

namespace CallService.DTOs
{
    public class StartCallResponseDto
    {
        public Guid CallId { get; set; }
        public Guid ConversationId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CallerName { get; set; } = "Unknown User";
        public CallType Type { get; set; }
        public CallStatus Status { get; set; }
        public bool IsGroupCall { get; set; }
        public bool ReceiverOffline { get; set; }
    }
}
