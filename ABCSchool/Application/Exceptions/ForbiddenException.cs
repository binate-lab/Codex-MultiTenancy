using System.Net;

namespace Application.Exceptions
{
    public class ForbiddenException : Exception
    {
        public List<string> ErrorMessages { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public ForbiddenException(List<string> errorMessages = default, HttpStatusCode statusCode = HttpStatusCode.Forbidden)
        {
            ErrorMessages = errorMessages;
            StatusCode = statusCode;
        }
    }
}
