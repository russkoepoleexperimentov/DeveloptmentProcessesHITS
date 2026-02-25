using Microsoft.AspNetCore.Http;

namespace Common.Exceptions
{
    public class InvalidAuthenticationDataException : StatusCodeException
    {
        public InvalidAuthenticationDataException() : base() { }
        public InvalidAuthenticationDataException(string message) : base(message) { }

        public override int StatusCode => StatusCodes.Status400BadRequest;
    }
}