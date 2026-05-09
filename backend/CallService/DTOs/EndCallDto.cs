using SharedService.Models;
namespace CallService.DTOs
{
    public class EndCallDto
    {
        public CallStatus Reason { get; set; } = CallStatus.Ended;
    }
}
