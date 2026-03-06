using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Common
{
    public static class HttpContextExtensions
    {
        public static Guid? GetUserId(this HttpContext context)
        {
            var str = context.User?.Claims?.FirstOrDefault(c => c.Type.Equals(ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))?.Value;
            return str == null ? null : new Guid(str);
        }
    }
}
