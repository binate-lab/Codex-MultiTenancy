namespace TrajanEcole.Shared.Library.Models.Responses.Token
{
    public class TokenResponse
    {
        public string Jwt { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryDate { get; set; }
    }
}
