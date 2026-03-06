
using Microsoft.AspNetCore.Http;

namespace Common.Exceptions
{
    public class EntryExistsException : StatusCodeException
    {
        public override int StatusCode => StatusCodes.Status422UnprocessableEntity;

        public EntryExistsException() : base() { }
        public EntryExistsException(string message) : base(message) { }
    }
}