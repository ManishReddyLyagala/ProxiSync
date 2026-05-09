using SharedService.Models;

namespace CallService.DTOs
{
    public class CallSessionDetailsDto
    {
            public Guid CallId { get; set; }
            public Guid ConversationId { get; set; }
            public string RoomName { get; set; } = string.Empty;

            public CallType Type { get; set; }
            public CallStatus Status { get; set; }

            public bool IsGroupCall { get; set; }
            public string StartedByUserId { get; set; } = string.Empty;

            public DateTimeOffset StartedAt { get; set; }
            public DateTimeOffset? EndedAt { get; set; }

            public List<CallParticipantDto> Participants { get; set; } = new();
        
    }

    public class CallParticipantDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        public ParticiapantCallStatus Status { get; set; } = ParticiapantCallStatus.Invited;

        public bool Joined { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }

        public bool Left { get; set; }
        public DateTimeOffset? LeftAt { get; set; }

        public bool IsMicEnabled { get; set; }
        public bool IsVideoEnabled { get; set; }

        public DateTimeOffset? InvitedAt { get; set; }
    }
}
