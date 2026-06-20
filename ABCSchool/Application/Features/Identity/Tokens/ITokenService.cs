namespace Application.Features.Identity.Tokens
{
    public interface ITokenService
    {
        Task<TokenResponse> LoginAsync(TokenRequest request);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<TokenResponse> SelectSchoolAsync(SelectSchoolRequest request);
    }
}
