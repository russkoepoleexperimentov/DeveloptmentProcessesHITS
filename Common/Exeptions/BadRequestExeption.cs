using Microsoft.AspNetCore.Http;

namespace Common.Exceptions
{
    public class BadRequestException : StatusCodeException
    {
        public override int StatusCode => StatusCodes.Status400BadRequest;

        public BadRequestException() : base() { }
        public BadRequestException(string message) : base(message) { }

    }
}