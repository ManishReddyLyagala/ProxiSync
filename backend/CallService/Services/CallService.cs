using CallService.DTOs;
using CallService.Hubs;
using CallService.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedService.Data;
using SharedService.Models;
using static CallService.DTOs.CallHistoryDto;

namespace CallService.Services
{
    public class CallService: ICallService
    {
        private readonly AppDbContext _db;
        private readonly ILiveKitTokenService _tokenService;
        private readonly IHubContext<CallHub> _hub;

        public CallService(AppDbContext db, ILiveKitTokenService tokenService, IHubContext<CallHub> hub)
        {
            _db = db;
            _tokenService = tokenService;
            _hub = hub;
        }

        public async Task<List<CallLogDto>> GetHistoryAsync(string userId)
        {
            return await _db.CallSessions
                .Include(c => c.Conversation) 
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                    .Where(c => c.StartedByUserId == userId || c.Participants.Any(p => p.UserId == userId))
                .OrderByDescending(c => c.StartedAt)
                .Select(c => new CallLogDto
                {
                    Id = c.CallId.ToString(),
                    ConversationId = c.ConversationId,

                    DisplayName = c.IsGroupCall
                        ? (c.Conversation.Name ?? "Group Call")
                        : c.Participants.FirstOrDefault(p => p.UserId != userId)!.User.DisplayName ?? "User",

                    ProfilePictureUrl = c.IsGroupCall
                        ? "/assets/group-icon.jpg"
                        : c.Participants.FirstOrDefault(p => p.UserId != userId)!.User.ProfilePictureUrl ?? "/assets/avatar.png",

                    Type = c.Type.ToString(),
                    Status = c.Status,
                    StartedAt = c.StartedAt.DateTime,
                    IsIncoming = c.StartedByUserId != userId,
                    IsGroupCall = c.IsGroupCall,

                    Participants = c.Participants.Select(p => new CallHistoryParticipantDto
                    {
                        UserId = p.UserId,
                        DisplayName = p.User.DisplayName ?? p.User.UserName ?? "User",
                        ProfilePictureUrl = p.User!.ProfilePictureUrl ?? "/assets/avatar.png",
                        Status = p.Status,
                        IsOnline = p.User.IsOnline
                    }).ToList()
                })
                .ToListAsync();
        }

        // callmissed event trigger have to add
        public async Task<StartCallResponseDto> StartCallAsync(StartCallDto dto, string callerUserId)
        {
            // Ensure participants list doesn't contain caller
            dto.ParticipantUserIds = dto.ParticipantUserIds
                .Where(x => x != callerUserId)
                .Distinct()
                .ToList();

            // Determine group call
            var totalParticipants = dto.ParticipantUserIds.Count + 1;
            var isGroup = totalParticipants > 2;

            // TODO: check if reciver is already in any other call if yes reject new call request
            //if (!isGroup)
            //{
            //    if (_db.CallParticipants.dto.ParticipantUserIds[0])
            //}
            var callId = Guid.NewGuid();
            var roomName = $"call_{callId:N}";

            var session = new CallSession
            {
                CallId = callId,
                ConversationId = dto.ConversationId,
                RoomName = roomName,
                Type = dto.Type,
                Status = CallStatus.Ringing,
                IsGroupCall = isGroup,
                StartedByUserId = callerUserId,
                StartedAt = DateTimeOffset.UtcNow
            };

            // Add caller participant
            session.Participants.Add(new CallParticipant
            {
                CallId = session.CallId,
                UserId = callerUserId,
                Status = ParticiapantCallStatus.Joined,
                Joined = true,
                InvitedAt = DateTimeOffset.UtcNow,
                JoinedAt = DateTimeOffset.UtcNow,
                IsMicEnabled = dto.CallerMicEnabled,
                IsVideoEnabled = dto.CallerVideoEnabled
            });

            // Add invited participants
            foreach (var userId in dto.ParticipantUserIds)
            {
                session.Participants.Add(new CallParticipant
                {
                    CallId = session.CallId,
                    UserId = userId,
                    Status = ParticiapantCallStatus.Invited,
                    Joined = false,
                    Left = false,
                    InvitedAt = DateTimeOffset.UtcNow,
                    IsMicEnabled = false,
                    IsVideoEnabled = false
                });
            }

            if (dto.ParticipantUserIds.Count == 1)
            {
                var receiverId = dto.ParticipantUserIds[0];
                var conns = CallHub.GetConnections(receiverId);

                if (conns.Count == 0)
                {
                    //: Save to DB anyway so they see a "Missed Call" later
                    session.Participants.Find(p => p.UserId == receiverId)!.Status = ParticiapantCallStatus.Missed;
                    _db.CallSessions.Add(session);
                    await _db.SaveChangesAsync();

                    return new StartCallResponseDto
                    {
                        ReceiverOffline = true,
                        CallId = session.CallId
                    };
                }
            }


            // SignalR group for call
            // (frontend will join this group once it knows callId)
            // We still notify invited users directly:
             var callerDetails = await _db.Users.FirstOrDefaultAsync(u => u.Id == session.StartedByUserId);
            foreach (var targetUserId in dto.ParticipantUserIds)
            {
                var conns = CallHub.GetConnections(targetUserId);
                if (conns.Count == 0)
                {
                    session.Participants.Find(p => p.UserId == targetUserId)!.Status = ParticiapantCallStatus.Missed;
                    continue;
                }

                await _hub.Clients.Clients(conns).SendAsync("IncomingCall", new
                {
                    callId = session.CallId,
                    conversationId = session.ConversationId,
                    roomName = session.RoomName,
                    type = session.Type.ToString(),
                    startedByUserId = session.StartedByUserId,
                    callerName = callerDetails!.UserName ?? callerDetails!.DisplayName,
                    isGroupCall = session.IsGroupCall,
                    startedAt = session.StartedAt
                });
            }

            _db.CallSessions.Add(session);
            await _db.SaveChangesAsync();

            // Signal: call started group
            await _hub.Clients.Group($"call-{session.CallId}")
                .SendAsync("CallStarted", new { callId = session.CallId });

            return new StartCallResponseDto
            {
                CallId = session.CallId,
                ConversationId = session.ConversationId,
                RoomName = session.RoomName,
                CallerName = callerDetails!.UserName ?? callerDetails!.DisplayName,
                Type = session.Type,
                Status = session.Status,
                IsGroupCall = session.IsGroupCall
            };
        }

        public async Task<GenerateTokenResponseDto> GenerateTokenAsync(Guid callId, string userId)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null)
                throw new Exception("Call not found");

            // ensure user is participant
            var participant = call.Participants.FirstOrDefault(x => x.UserId == userId);
            if (participant == null)
                throw new Exception("User not part of this call");

            if (call.Status == CallStatus.Ended || call.Status == CallStatus.Missed)
                throw new Exception("Call already ended");

            var userDetails = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            string displayName = userDetails!.DisplayName ?? userDetails!.UserName ?? "Unknown User";
            var token = _tokenService.CreateToken(call.RoomName, userId, displayName);

            return new GenerateTokenResponseDto
            {
                CallId = call.CallId,
                RoomName = call.RoomName,
                Token = token,
                LiveKitUrl = _tokenService.GetLiveKitUrl()
            };
        }

        public async Task<bool> JoinCallAsync(Guid callId, string userId, JoinCallDto dto)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null) return false;
            if (call.EndedAt != null || call.Status == CallStatus.Ended || call.Status == CallStatus.Missed) return false;

            var participant = call.Participants.FirstOrDefault(x => x.UserId == userId);
            if (participant == null) return false;

            if(participant.Status == ParticiapantCallStatus.Rejected || participant.Status == ParticiapantCallStatus.Missed || participant.Status == ParticiapantCallStatus.Left)
            {
                return false;
            }

            participant.Joined = true;
            participant.JoinedAt = DateTimeOffset.UtcNow;
            participant.Left = false;
            participant.LeftAt = null;
            participant.Status = ParticiapantCallStatus.Joined;

            participant.IsMicEnabled = dto.MicEnabled;
            participant.IsVideoEnabled = dto.VideoEnabled;

            var callStateBefore = call.Status;
            // If call is still ringing, make it ongoing when 2nd person joins
            if (call.Status == CallStatus.Ringing)
            {
                call.Status = CallStatus.Ongoing;
            }

            await _db.SaveChangesAsync();

            var callerId = call.StartedByUserId;
            if (callerId != userId && !call.HasAnyReceiverJoined)
            {
                call.HasAnyReceiverJoined = true;
                await _db.SaveChangesAsync();
                await SendToUser(callerId, "CallAccepted", new
                {
                    callId = call.CallId,
                    acceptedBy = userId
                });
            }

            await _hub.Clients.Group($"call-{call.CallId}")
               .SendAsync("ParticipantStatusChanged", new
               {
                   callId = call.CallId,
                   userId = userId,
                   status = participant.Status
               });
            if (callStateBefore != call.Status)
            {
                await _hub.Clients.Group($"call-{call.CallId}")
                .SendAsync("CallStatusChanged", new
                {
                    callId = call.CallId,
                    status = call.Status.ToString()
                });
            }

            // Stop ringing for the user who accepted
            await SendToUser(userId, "StopRinging", new { callId });

            // Stop ringing for caller too (so ringtone stops on outgoing)
            await SendToUser(callerId, "StopRinging", new { callId });
            return true;
        }

        public async Task<bool> RejectCallAsync(Guid callId, string userId)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null) return false;
            if (call.EndedAt != null || call.Status == CallStatus.Ended) return false;

            var p = call.Participants.FirstOrDefault(x => x.UserId == userId);
            if (p == null) return false;

            // Only invited can reject
            if (p.Status != ParticiapantCallStatus.Invited) return false;

            p.Status = ParticiapantCallStatus.Rejected;
            p.Left = true;
            p.LeftAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            // Notify call group
            await _hub.Clients.Group($"call-{call.CallId}")
                .SendAsync("ParticipantStatusChanged", new
                {
                    callId = call.CallId,
                    userId,
                    status = CallStatus.Rejected
                });
            // Notify caller directly
            await SendToUser(call.StartedByUserId, "CallRejected", new
            {
                callId = call.CallId,
                rejectedBy = userId
            });

            await SendToUser(userId, "StopRinging", new { callId });
            await SendToUser(call.StartedByUserId, "StopRinging", new { callId });
            await TryEndCallIfNoActiveAsync(call);

            return true;
        }

        public async Task<bool> LeaveCallAsync(Guid callId, string userId)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null) return false;
            if (call.EndedAt != null || call.Status == CallStatus.Ended) return true;

            var p = call.Participants.FirstOrDefault(x => x.UserId == userId);
            if (p == null) return false;

            if (!p.Joined || p.Left) return true;

            p.Left = true;
            p.LeftAt = DateTimeOffset.UtcNow;
            p.Status = ParticiapantCallStatus.Left;

            await _db.SaveChangesAsync();

            if(call.StartedByUserId == userId || (call.Participants.FindAll((x) => x.UserId != userId).Count() <= 1 && !call.IsGroupCall))
            {
                await _hub.Clients.Group($"call-{call.CallId}")
                .SendAsync("CallEnded", new
                {
                    callId = call.CallId,
                    status = call.Status.ToString(),
                    endedAt = call.EndedAt
                });
            }

            await _hub.Clients.Group($"call-{call.CallId}")
                .SendAsync("ParticipantStatusChanged", new
                {
                    callId = call.CallId,
                    userId,
                    status = p.Status
                });

            await TryEndCallIfNoActiveAsync(call);
            return true;
        }

        public async Task<bool> CancelCallAsync(Guid callId, string userId)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null) return false;
            if (call.StartedByUserId != userId) return false;

            // only cancel while ringing
            if (call.HasAnyReceiverJoined) return false;

            call.Status = CallStatus.Ended;
            call.EndedAt = DateTimeOffset.UtcNow;

            foreach (var p in call.Participants)
            {
                    //p.Left = true;
                    //p.LeftAt = DateTimeOffset.UtcNow;
                    if (p.UserId == call.StartedByUserId) continue;
                    if (p.Status == ParticiapantCallStatus.Invited)
                    {
                        p.Status = ParticiapantCallStatus.Missed;
                    }
            }

            await _db.SaveChangesAsync();

            await _hub.Clients.Group($"call-{call.CallId}")
             .SendAsync("CallCancelled", new
             {
                 callId = call.CallId,
                 cancelledBy = call.StartedByUserId
             });

            // Stop ringing for everyone
            foreach (var p in call.Participants)
            {
                await SendToUser(p.UserId, "StopRinging", new { callId });
            }

            return true;
        }
        public async Task<bool> UpdateParticipantMediaAsync(Guid callId, string userId, ParticipantMediaDto dto)
        {
            var participant = await _db.CallParticipants
                .FirstOrDefaultAsync(x => x.CallId == callId && x.UserId == userId);

            if (participant == null) return false;

            participant.IsMicEnabled = dto.IsMicEnabled;
            participant.IsVideoEnabled = dto.IsVideoEnabled;

            await _db.SaveChangesAsync();

            await _hub.Clients.Group($"call-{callId}")
               .SendAsync("ParticipantMediaUpdated", new
               {
                   callId,
                   userId,
                   micEnabled = participant.IsMicEnabled,
                   videoEnabled = participant.IsVideoEnabled
               });
            return true;
        }

        public async Task<bool> EndCallAsync(Guid callId, string userId)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null) return false;

            // only starter can end in phase 1
            if (call.StartedByUserId != userId)
                return false;

            if (call.Status == CallStatus.Ended)
                return true;

            call.Status = CallStatus.Ended;
            call.EndedAt = DateTimeOffset.UtcNow;

            // Mark all participants as left if not already
            foreach (var p in call.Participants)
            {
                if (!p.Left)
                {
                    p.Left = true;
                    p.LeftAt = DateTimeOffset.UtcNow;
                    if(p.Status == ParticiapantCallStatus.Invited)
                    {
                        p.Status = ParticiapantCallStatus.Missed;
                    }
                    else
                    {
                        p.Status = ParticiapantCallStatus.Left;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await _hub.Clients.Group($"call-{call.CallId}")
                 .SendAsync("CallEnded", new
                 {
                     callId = call.CallId,
                     status = call.Status.ToString(),
                     endedAt = call.EndedAt
                 });
            await _hub.Clients.Group($"call-{call.CallId}")
                .SendAsync("CallCancelled", new
                {
                    callId = call.CallId,
                    cancelledBy = call.StartedByUserId
                });

            // Stop ringing for everyone
            foreach (var p in call.Participants.Where(p => p.Status == ParticiapantCallStatus.Invited))
            {
                await SendToUser(p.UserId, "StopRinging", new { callId });
            }
            return true;
        }

        public async Task<CallSessionDetailsDto?> GetCallAsync(Guid callId, string userId)
        {
            var call = await _db.CallSessions
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.CallId == callId);

            if (call == null) return null;

            // must be participant
            if (!call.Participants.Any(x => x.UserId == userId))
                return null;

            return new CallSessionDetailsDto
            {
                CallId = call.CallId,
                ConversationId = call.ConversationId,
                RoomName = call.RoomName,
                Type = call.Type,
                Status = call.Status,
                IsGroupCall = call.IsGroupCall,
                StartedByUserId = call.StartedByUserId,
                StartedAt = call.StartedAt,
                EndedAt = call.EndedAt,
                Participants = call.Participants.Select(p => new CallParticipantDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Status = p.Status,
                    Joined = p.Joined,
                    JoinedAt = p.JoinedAt,
                    Left = p.Left,
                    LeftAt = p.LeftAt,
                    IsMicEnabled = p.IsMicEnabled,
                    IsVideoEnabled = p.IsVideoEnabled,
                    InvitedAt = p.InvitedAt
                }).ToList()
            };
        }

        private async Task TryEndCallIfNoActiveAsync(CallSession call)
        {
            // Active = joined and not left
            var activeParticipants = call.Participants.Where(p => p.Joined && !p.Left).ToList();

            if (activeParticipants.Count < 2)
            {
                call.Status = CallStatus.Ended;
                call.EndedAt = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync();

                await _hub.Clients.Group($"call-{call.CallId}")
                    .SendAsync("CallEnded", new { callId = call.CallId });
            }
        }

        private Task SendToUser(string userId, string eventName, object payload)
        {
            var conns = CallHub.GetConnections(userId);
            if (conns.Count == 0) return Task.CompletedTask;

            return _hub.Clients.Clients(conns).SendAsync(eventName, payload);
        }

    }
}
