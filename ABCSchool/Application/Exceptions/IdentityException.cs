using System.Net;

namespace Application.Exceptions
{
    public class IdentityException : Exception
    {
        public List<string> ErrorMessages { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public IdentityException(List<string> errorMessages = default, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            ErrorMessages = errorMessages;
            StatusCode = statusCode;
        }
    }
}
