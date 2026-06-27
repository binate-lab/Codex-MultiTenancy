using TrajanEcole.Shared.Library.Models.Requests.Token;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Identity
{
    public interface ITokenService
    {
        Task<IResponseWrapper> LoginAsync(string tenant, TokenRequest request);
        Task<IResponseWrapper> SelectSchoolAsync(string codeEts);
        Task<IResponseWrapper> LogoutAsync();
        Task<string> RefreshTokenAsync();
        Task<string> TryForceRefreshTokenAsync();
    }
}
