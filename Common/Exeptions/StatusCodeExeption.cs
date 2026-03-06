
namespace Common.Exceptions
{
    public abstract class StatusCodeException : Exception
    {
        public abstract int StatusCode { get; }
        public StatusCodeException() : base() { }
        public StatusCodeException(string message) : base(message) { }
    }
}