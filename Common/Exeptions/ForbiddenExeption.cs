using Microsoft.AspNetCore.Http;

namespace Common.Exceptions
{
    public class ForbiddenException : StatusCodeException
    {
        public override int StatusCode => StatusCodes.Status403Forbidden;

        public ForbiddenException() : base() { }
        public ForbiddenException(string message) : base(message) { }
    }
}