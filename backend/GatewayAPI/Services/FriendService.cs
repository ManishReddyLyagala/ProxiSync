using Azure.Core;
using GatewayAPI.DTOs;
using GatewayAPI.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedService.Data;
using SharedService.DTOs;
using SharedService.Models;
using System.Linq.Expressions;

namespace GatewayAPI.Services
{
    public class FriendService : IFriendService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public FriendService(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        #region Projection Property
        // This "Map" tells EF Core exactly which columns to select and how to build the DTO.
        // It converts to a single SQL JOIN query.
        private static readonly Expression<Func<FriendRequest, FriendRequestDto>> FriendRequestProjection = fr => new FriendRequestDto
        {
            Id = fr.Id,
            FromUserId = fr.FromUserId,
            ToUserId = fr.ToUserId,
            Status = fr.Status,
            Message = fr.Message,
            SentAt = fr.SentAt,
            Sender = new UserDto
            {
                Id = fr.Sender.Id,
                DisplayName = fr.Sender.DisplayName,
                ProfilePictureUrl = fr.Sender.ProfilePictureUrl,
                IsOnline = fr.Sender.IsOnline,
                Bio = fr.Sender.Bio
            },
            Receiver = new UserDto
            {
                Id = fr.Receiver.Id,
                DisplayName = fr.Receiver.DisplayName,
                ProfilePictureUrl = fr.Receiver.ProfilePictureUrl,
                IsOnline = fr.Receiver.IsOnline,
                Bio = fr.Receiver.Bio
            }
        };
        #endregion

        public async Task<Response<FriendRequestDto>> SendRequestAsync(string fromUserId, string toUserId, string? message)
        {
            if (fromUserId == toUserId)
                return Response<FriendRequestDto>.Fail("Cannot send request to yourself.");

            // check existing pending
            var existing = await _db.FriendRequests
                .FirstOrDefaultAsync(fr =>
                    (fr.FromUserId == fromUserId && fr.ToUserId == toUserId || fr.ToUserId == fromUserId && fr.FromUserId == toUserId ) && fr.Status == FriendRequestStatus.Pending);

            if (existing != null)
                return Response<FriendRequestDto>.Fail("Request already pending.");

            // optional: check if already accepted
            var alreadyAccepted = await _db.FriendRequests
                .AnyAsync(fr =>
                    ((fr.FromUserId == fromUserId && fr.ToUserId == toUserId) ||
                     (fr.FromUserId == toUserId && fr.ToUserId == fromUserId)) &&
                    fr.Status == FriendRequestStatus.Accepted);
            if (alreadyAccepted)
                return Response<FriendRequestDto>.Fail("You are already contacts.");

            var fr = new FriendRequest
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Message = message,
                SentAt = DateTimeOffset.UtcNow,
                Status = FriendRequestStatus.Pending
            };

            _db.FriendRequests.Add(fr);
            await _db.SaveChangesAsync();

            return await GetRequestByIdAsync(fr.Id);
        }

        public async Task<Response<FriendRequestDto>> RespondRequestAsync(Guid requestId, string responderId, bool accept)
        {
            var fr = await _db.FriendRequests.FindAsync(requestId);
            if (fr == null) return Response<FriendRequestDto>.Fail("Request not found.");
            if (fr.ToUserId != responderId) return Response<FriendRequestDto>.Fail("Unauthorized.");
            if (fr.Status != FriendRequestStatus.Pending) return Response<FriendRequestDto>.Fail("Already processed.");

            fr.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;
            fr.RespondedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            return await GetRequestByIdAsync(fr.Id);
        }

        // recived pending requests
        public async Task<Response<IEnumerable<FriendRequestDto>>> GetPendingRequestsAsync(string userId)
        {
            var requests =  await _db.FriendRequests
                 .Where(fr => fr.ToUserId == userId && fr.Status == FriendRequestStatus.Pending)
                 .OrderByDescending(fr => fr.SentAt)
                 .Select(FriendRequestProjection)
                 .ToListAsync();
            return Response<IEnumerable<FriendRequestDto>>.Ok(requests);
        }

        public async Task<Response<List<FriendRequestDto>>> ListSentRequestsAsync(string userId)
        {
            var requests = await _db.FriendRequests
                .Where(r => r.FromUserId == userId && (r.Status == FriendRequestStatus.Pending || r.Status == FriendRequestStatus.Rejected))
                .OrderByDescending(r => r.SentAt)
                .Select(FriendRequestProjection)
                .ToListAsync();

            return Response<List<FriendRequestDto>>.Ok(requests);
        }

        public async Task<Response<IEnumerable<FriendRequestDto>>> GetContactsAsync(string userId)
        {
            // Gets accepted friends in one query by selecting either the Sender or Receiver 
            // depending on which one ISN'T the current user.
            var result =  await _db.FriendRequests
                .Where(fr => (fr.FromUserId == userId || fr.ToUserId == userId) && (fr.Status == FriendRequestStatus.Accepted || fr.Status == FriendRequestStatus.UnFollowed))
                .Select(FriendRequestProjection)
                .ToListAsync();
            return Response<IEnumerable<FriendRequestDto>>.Ok(result);
        }

        public async Task<Response<IEnumerable<ContactListDto>>> GetMutualContactListAsync(string userId)
        {
            // 1. Get IDs of people who accepted the request, excluding the current user
            var friendsIds = await _db.FriendRequests
                .Where(fr => (fr.FromUserId == userId || fr.ToUserId == userId) && fr.Status == FriendRequestStatus.Accepted)
                .Select(fr => fr.FromUserId == userId ? fr.ToUserId : fr.FromUserId)
                .Distinct()
                .ToListAsync();

            if (!friendsIds.Any())
            {
                return Response<IEnumerable<ContactListDto>>.Ok(new List<ContactListDto>(), "No contacts found.");
            }

            // 2. Fetch user details for those IDs and map to DTO
            var contacts = await _db.Users
                .Where(u => friendsIds.Contains(u.Id))
                .Select(u => new ContactListDto
                {
                    UserId = u.Id,
                    UserName = u.DisplayName ?? u.UserName,
                    ProfilePictureUrl = u.ProfilePictureUrl // Ensure this property exists in your ApplicationUser
                })
                .ToListAsync();

            return Response<IEnumerable<ContactListDto>>.Ok(contacts);
        }

        public async Task<Response<bool>> CancelRequestAsync(Guid requestId, string userId)
        {
            var fr = await _db.FriendRequests.FindAsync(requestId);
            if (fr == null) return Response<bool>.Fail("Request not found.");
            if (fr.FromUserId != userId) return Response<bool>.Fail("Only the sender can cancel the request.");
            if (fr.Status != FriendRequestStatus.Pending) return Response<bool>.Fail("Only pending requests can be cancelled.");

            //fr.Status = FriendRequestStatus.Cancelled;
            //fr.RespondedAt = DateTimeOffset.UtcNow;
            //_db.FriendRequests.Update(fr);
            _db.FriendRequests.Remove(fr);
            await _db.SaveChangesAsync();

            return Response<bool>.Ok(true, "Request cancelled.");
        }

        // ✅ UnFollow Friend request
        public async Task<Response<FriendRequestDto>> ToggleFollowRequestAsync(Guid requestId, string currentUserId)
        {
            var req = await _db.FriendRequests.FindAsync(requestId);
            var curUserId = Guid.TryParse(currentUserId, out var userId) ? userId : Guid.Empty;

            if (req == null || (req.FromUserId != currentUserId && req.ToUserId != currentUserId))
                return Response<FriendRequestDto>.Fail("Request not found or unauthorized");

            if(req.Status == FriendRequestStatus.Accepted && (req.ActionByUserId == null || req.ActionByUserId == Guid.Empty))
            {
                req.Status = FriendRequestStatus.UnFollowed;
                req.ActionByUserId = curUserId;
            }
            else if(req.Status == FriendRequestStatus.UnFollowed)
            {
                if(req.ActionByUserId == curUserId)
                {
                    req.Status = FriendRequestStatus.Accepted;
                    req.ActionByUserId = null;
                }
                else
                    return Response<FriendRequestDto>.Fail("Only the user who unfollowed can re-follow.");
            }
            else
            {
                return Response<FriendRequestDto>.Fail("Cannot toggle follow on non-accepted requests.");
            }
            req.RespondedAt = DateTimeOffset.UtcNow;
            _db.FriendRequests.Update(req);
            await _db.SaveChangesAsync();

            return Response<FriendRequestDto>.Ok(null, req.Status == FriendRequestStatus.UnFollowed ? "UnFollow Successful.": "ReFollow Successful.");
        }

        //public async Task<Response<string>> UnBlockUserAsync(Guid requestId, string userId)
        //{
        //    var req = await _db.FriendRequests.FindAsync(requestId);
        //    if (req == null || (req.FromUserId != userId && req.ToUserId != userId))
        //        return Response<string>.Fail("Request not found or unauthorized");
        //    if(req.Status != FriendRequestStatus.Blocked)
        //        return Response<string>.Fail("User is not blocked.");

        //    req.Status = FriendRequestStatus.Accepted;
        //    req.RespondedAt = DateTimeOffset.UtcNow;
        //    _db.FriendRequests.Update(req);
        //    await _db.SaveChangesAsync();

        //    return Response<string>.Ok(null, "User has UnBlocked.");
        //}

        // ✅ Check status
        public async Task<Response<string>> CheckStatusAsync(string fromUserId, string toUserId)
        {
            var req = await _db.FriendRequests
                .FirstOrDefaultAsync(r => (r.FromUserId == fromUserId && r.ToUserId == toUserId) || 
                                          (r.FromUserId == toUserId && r.ToUserId == fromUserId));

            return Response<string>.Ok(req?.Status.ToString() ?? "None");
        }

        private async Task<Response<FriendRequestDto>> GetRequestByIdAsync(Guid id)
        {
            var result = await _db.FriendRequests
                .Where(x => x.Id == id)
                .Select(FriendRequestProjection)
                .FirstOrDefaultAsync();

            return result != null ? Response<FriendRequestDto>.Ok(result) : Response<FriendRequestDto>.Fail("Not found");
        }
    }
}
