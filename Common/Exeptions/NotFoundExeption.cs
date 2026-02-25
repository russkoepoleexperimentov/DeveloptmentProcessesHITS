using Microsoft.AspNetCore.Http;

namespace Common.Exceptions
{
    public class NotFoundException : StatusCodeException
    {
        public override int StatusCode => StatusCodes.Status404NotFound;

        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
    }
}