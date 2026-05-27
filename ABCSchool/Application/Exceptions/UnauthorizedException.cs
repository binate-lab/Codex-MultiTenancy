using System.Net;

namespace Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public List<string> ErrorMessages { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public UnauthorizedException(List<string> errorMessages = default, HttpStatusCode statusCode = HttpStatusCode.Unauthorized)
        {
            StatusCode = statusCode;
            ErrorMessages = errorMessages;
        }
    }
}
