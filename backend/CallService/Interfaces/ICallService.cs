using CallService.DTOs;
using static CallService.DTOs.CallHistoryDto;

namespace CallService.Interfaces
{
    public interface ICallService
    {
        Task<List<CallLogDto>> GetHistoryAsync(string userId);
        Task<StartCallResponseDto> StartCallAsync(StartCallDto dto, string callerUserId);
        Task<GenerateTokenResponseDto> GenerateTokenAsync(Guid callId, string userId);
        Task<bool> JoinCallAsync(Guid callId, string userId, JoinCallDto dto);
        Task<bool> RejectCallAsync(Guid callId, string userId);
        Task<bool> LeaveCallAsync(Guid callId, string userId);
        Task<bool> CancelCallAsync(Guid callId, string userId);
        Task<CallSessionDetailsDto?> GetCallAsync(Guid callId, string userId);
        Task<bool> UpdateParticipantMediaAsync(Guid callId, string userId, ParticipantMediaDto dto);
        Task<bool> EndCallAsync(Guid callId, string userId);
    }
}
