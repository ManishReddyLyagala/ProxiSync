using CallService.Hubs;
using Microsoft.AspNetCore.SignalR;
using SharedService.Data;
using SharedService.Models;
using Microsoft.EntityFrameworkCore;

namespace CallService.Background
{
    public class CallRingTimeoutService: BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<CallHub> _hub;

        private readonly TimeSpan _ringTimeout = TimeSpan.FromSeconds(30);

        public CallRingTimeoutService(IServiceScopeFactory scopeFactory, IHubContext<CallHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTimeOffset.UtcNow;

                    var calls = await db.CallSessions
                        .Include(x => x.Participants)
                        .Where(x =>
                            x.EndedAt == null &&
                            (x.Status == CallStatus.Ringing || x.Status == CallStatus.Ongoing))
                        .ToListAsync(stoppingToken);

                    foreach (var call in calls)
                    {
                        bool updated = false;

                        // 1) Timeout per participant
                        foreach (var p in call.Participants)
                        {
                            if (!p.Joined && !p.Left && p.Status == ParticiapantCallStatus.Invited)
                            {
                                if (p.InvitedAt.Add(_ringTimeout) <= now)
                                {
                                    p.Status = ParticiapantCallStatus.Missed;
                                    p.Left = true;
                                    p.LeftAt = now;

                                    updated = true;

                                    await _hub.Clients.User(p.UserId)
                                        .SendAsync("StopRinging", new { callId = call.CallId });
                                }
                            }
                        }

                        // 2) Determine active participants
                        var activeCount = call.Participants.Count(p => p.Joined && !p.Left);
                        var hasActive = activeCount > 0;

                        // 3) Determine pending invites
                        var hasPendingInvite = call.Participants.Any(p => !p.Joined && !p.Left && p.Status == ParticiapantCallStatus.Invited);

                        // 4) WhatsApp rule for Direct calls:
                        // If the recipient missed/rejected and call is still ringing -> end it
                        if (!call.IsGroupCall && call.Status == CallStatus.Ringing)
                        {
                            var other = call.Participants.FirstOrDefault(x => x.UserId != call.StartedByUserId);

                            if (other != null && (other.Status == ParticiapantCallStatus.Missed || other.Status == ParticiapantCallStatus.Rejected))
                            {
                                call.Status = CallStatus.Missed;
                                call.EndedAt = now;

                                // caller should be marked left
                                var caller = call.Participants.FirstOrDefault(x => x.UserId == call.StartedByUserId);
                                if (caller != null && !caller.Left)
                                {
                                    caller.Left = true;
                                    caller.LeftAt = now;
                                    caller.Status = ParticiapantCallStatus.Left;
                                }

                                updated = true;

                                await db.SaveChangesAsync(stoppingToken);

                                await _hub.Clients.Group($"call-{call.CallId}")
                                    .SendAsync("CallMissed", new
                                    {
                                        callId = call.CallId,
                                        status = call.Status.ToString(),
                                        endedAt = call.EndedAt
                                    }, stoppingToken);

                                continue;
                            }
                        }

                        // 5) End call if no active users AND no pending invites
                        if (!hasActive && !hasPendingInvite)
                        {
                            if (call.Status == CallStatus.Ringing)
                                call.Status = CallStatus.Missed;
                            else
                                call.Status = CallStatus.Ended;

                            call.EndedAt = now;
                            updated = true;

                            await db.SaveChangesAsync(stoppingToken);

                            await _hub.Clients.Group($"call-{call.CallId}")
                                .SendAsync("CallMissed", new
                                {
                                    callId = call.CallId,
                                    status = call.Status.ToString(),
                                    endedAt = call.EndedAt
                                });

                            continue;
                        }

                        // 6) If anyone joined, call becomes ongoing
                        if (hasActive && call.Status == CallStatus.Ringing)
                        {
                            call.Status = CallStatus.Ongoing;
                            updated = true;

                            await _hub.Clients.Group($"call-{call.CallId}")
                                .SendAsync("CallStatusChanged", new
                                {
                                    callId = call.CallId,
                                    status = call.Status.ToString()
                                });
                        }

                        // 7) Save + broadcast participant changes
                        if (updated)
                        {
                            await db.SaveChangesAsync(stoppingToken);

                            await _hub.Clients.Group($"call-{call.CallId}")
                                .SendAsync("ParticipantStatusChanged", new
                                {
                                    callId = call.CallId
                                });
                        }
                    }
                }
                catch
                {
                    // keep service alive
                }

                await Task.Delay(3000, stoppingToken);
            }
        }   
    }
}
