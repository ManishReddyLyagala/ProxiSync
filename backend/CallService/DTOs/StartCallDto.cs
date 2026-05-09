using SharedService.Models;
namespace CallService.DTOs
{
    public class StartCallDto
    {
        public Guid ConversationId { get; set; }

        // Audio or Video
        public CallType Type { get; set; } = CallType.Audio;

        // list of users invited (not including current user)
        public List<string> ParticipantUserIds { get; set; } = new();

        // initial settings for the caller
        public bool CallerMicEnabled { get; set; } = true;
        public bool CallerVideoEnabled { get; set; } = false;
    }
}
