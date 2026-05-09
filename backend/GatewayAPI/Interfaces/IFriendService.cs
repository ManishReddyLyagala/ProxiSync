using GatewayAPI.DTOs;
using SharedService.DTOs;
using SharedService.Models;

namespace GatewayAPI.Interfaces
{
    public interface IFriendService
    {
        Task<Response<FriendRequestDto>> SendRequestAsync(string fromUserId, string toUserId, string? message);
        Task<Response<FriendRequestDto>> RespondRequestAsync(Guid requestId, string responderId, bool accept);
        Task<Response<IEnumerable<FriendRequestDto>>> GetPendingRequestsAsync(string userId);
        Task<Response<List<FriendRequestDto>>> ListSentRequestsAsync(string userId);
        Task<Response<IEnumerable<FriendRequestDto>>> GetContactsAsync(string userId);
        Task<Response<IEnumerable<ContactListDto>>> GetMutualContactListAsync(string userId);
        Task<Response<bool>> CancelRequestAsync(Guid requestId, string userId);

        Task<Response<FriendRequestDto>> ToggleFollowRequestAsync(Guid requestId, string userId);
        //Task<Response<string>> UnBlockUserAsync(Guid requestId, string userId);
        Task<Response<string>> CheckStatusAsync(string fromUserId, string toUserId);
    }
}
