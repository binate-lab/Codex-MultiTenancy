using TrajanEcole.Shared.Library.Models.Requests.Identity;
using TrajanEcole.Shared.Library.Models.Responses.Identity;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Identity
{
    public interface IRoleService
    {
        Task<IResponseWrapper<List<RoleResponse>>> GetRolesAsync();
        Task<IResponseWrapper<string>> CreateAsync(CreateRoleRequest request);
        Task<IResponseWrapper<string>> UpdateAsync(UpdateRoleRequest request);
        Task<IResponseWrapper<string>> DeleteAsync(string roleId);
        Task<IResponseWrapper<RoleResponse>> GetRoleWithoutPermissionsAsync(string roleId);
        Task<IResponseWrapper<RoleResponse>> GetRoleWithPermissionsAsync(string roleId);
        Task<IResponseWrapper<string>> UpdatePermissionsAsync(UpdateRolePermissionsRequest request);
    }
}
