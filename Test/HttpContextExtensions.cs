using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Tests
{
    public static class HttpContextExtensions
    {
        public static Guid? GetUserId(this HttpContext httpContext)
        {
            var claimNames = new[] { "userId", "sub", "id", ClaimTypes.NameIdentifier };

            foreach (var claimName in claimNames)
            {
                var claim = httpContext.User.FindFirst(claimName);
                if (claim != null && Guid.TryParse(claim.Value, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }
    }
}