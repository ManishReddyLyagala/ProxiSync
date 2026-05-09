using System.Collections.Concurrent;

namespace ChatService.Utils
{
    public class PresenceTracker
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();

        public static bool AddConnection(string userId, string connectionId)
        {
            bool isFirstConnected = false;
            var connections = _connections.GetOrAdd(userId, _ =>
            {
                isFirstConnected = true;
                return new HashSet<string>();
            });
            
            lock (connections)
            {
                connections.Add(connectionId);
            }
            return isFirstConnected;
        }

        public static bool RemoveConnection(string userId, string connectionId)
        {
            bool isLastConnection = false;
            if (_connections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _connections.TryRemove(userId, out _);
                        isLastConnection = true;
                    }
                }
            }
            return isLastConnection;
        }

        public static string[] GetOnlineUserIds() => _connections.Keys.ToArray();

        public static bool HasConnections(string userId)
        {
            return _connections.TryGetValue(userId, out var conns);
        }

        public static IEnumerable<string> GetConnections(string userId)
        {
            if (_connections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.ToList();
                }
            }
            return Enumerable.Empty<string>();
        }
    }
}
