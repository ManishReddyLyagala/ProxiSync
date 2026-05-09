using System.Collections.Concurrent;
using System.Security.Claims;

namespace ChatService.Utils
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _limit;
        private readonly TimeSpan _window;

        private static readonly ConcurrentDictionary<string, (int count, DateTimeOffset WindowStart)> _store = new();
        public RateLimitMiddleware(RequestDelegate next, int limit = 45, int windowSeconds = 30)
        {
            _next = next;
            _limit = limit;
            _window = TimeSpan.FromSeconds(windowSeconds);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var now = DateTimeOffset.UtcNow;
                    _store.AddOrUpdate(userId, _ => (1, now),
                        (_, old) =>
                        {
                            if (now - old.WindowStart > _window)
                            {
                                return (1, now);
                            }

                            return (old.count + 1, old.WindowStart);
                        });
                    var updated = _store[userId];
                    if (updated.count > _limit)
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded. Try later.\"}");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
        public static class RateLimitMiddlewareExtensions
        {
            public static IApplicationBuilder UseSimpleRateLimiting(this IApplicationBuilder builder, int limit = 30, int windowSeconds = 30)
            {
                return builder.UseMiddleware<RateLimitMiddleware>(limit, windowSeconds);
            }
        }
    
}
