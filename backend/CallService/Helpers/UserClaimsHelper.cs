using System.Security.Claims;

namespace CallService.Helpers
{
    public class UserClaimsHelper
    {
        public static string? GetUserId(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
