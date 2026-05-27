using ABCShared.Library.Models.Requests.Token;

namespace App.Infrastructure.Models
{
    public class LoginRequest : TokenRequest
    {
        public string Tenant { get; set; }
    }
}
