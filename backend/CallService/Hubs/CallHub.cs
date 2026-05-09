using CallService.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace CallService.Hubs
{
    [Authorize]
    public class CallHub : Hub
    {
        // userId -> list of connectionIds
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
                lock (connections)
                {
                    connections.Add(Context.ConnectionId);
                }
            }

            //var activeCallId = await _callService.GetActiveCallIdForUser(userId);
            //if (activeCallId != Guid.Empty)
            //{
            //    await Groups.AddToGroupAsync(Context.ConnectionId, $"call-{activeCallId}");
            //}

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId) && _userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                        _userConnections.TryRemove(userId, out _);
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Called by frontend when opening call screen.
        /// This allows us to send events to all participants using a call group.
        /// </summary>
        public async Task JoinCallGroup(string callId)
        {
            var userId = UserClaimsHelper.GetUserId(Context.User) ?? null;
            if (string.IsNullOrEmpty(userId)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"call-{callId}");
        }

        public async Task LeaveCallGroup(string callId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"call-{callId}");
        }

        /// <summary>
        /// Used by CallService (server side) to find user connections
        /// </summary>
        public static List<string> GetConnections(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.ToList();
                }
            }

            return new List<string>();
        }
    }
}
