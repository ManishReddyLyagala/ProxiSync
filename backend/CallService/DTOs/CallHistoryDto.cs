using SharedService.Models;

namespace CallService.DTOs
{
    public class CallHistoryDto
    {
        public class CallHistoryParticipantDto
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
            public string ProfilePictureUrl { get; set; }
            public ParticiapantCallStatus Status { get; set; } // "Joined", "Rejected", "Missed", "Ringing"
            public bool IsOnline { get; set; }
        }

        public class CallLogDto
        {
            public string Id { get; set; }
            public Guid ConversationId { get; set; }
            public string DisplayName { get; set; }
            public string ProfilePictureUrl { get; set; }
            public string Type { get; set; }
            public CallStatus Status { get; set; }
            public DateTime StartedAt { get; set; }
            public bool IsIncoming { get; set; }
            public bool IsGroupCall { get; set; }
            public List<CallHistoryParticipantDto> Participants { get; set; } = new();
        }
    }
}
