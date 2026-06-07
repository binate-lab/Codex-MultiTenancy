using TrajanEcole.Shared.Library.Models.Requests.Identity;
using TrajanEcole.Shared.Library.Models.Responses.Identity;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Identity
{
    public interface IUserService
    {
        Task<IResponseWrapper<string>> UpdateUserAsync(UpdateUserRequest request);
        Task<IResponseWrapper<string>> ChangeUserPasswordAsync(ChangePasswordRequest request);
        Task<IResponseWrapper<List<UserResponse>>> GetUsersAsync();
        Task<IResponseWrapper<UserResponse>> GetByIdAsync(string userId);
        Task<IResponseWrapper<string>> RegisterUserAsync(CreateUserRequest request);
        Task<IResponseWrapper<List<UserRoleResponse>>> GetUserRolesAsync(string userId);
        Task<IResponseWrapper<string>> UpdateUserRolesAsync(string userId, UserRolesRequest request);
        Task<IResponseWrapper<string>> ChangeUserStatusAsync(ChangeUserStatusRequest request);
    }
}
