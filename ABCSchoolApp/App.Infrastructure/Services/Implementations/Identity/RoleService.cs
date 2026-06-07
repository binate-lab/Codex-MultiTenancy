using TrajanEcole.Shared.Library.Models.Requests.Identity;
using TrajanEcole.Shared.Library.Models.Responses.Identity;
using TrajanEcole.Shared.Library.Wrappers;
using App.Infrastructure.Extensions;
using App.Infrastructure.Services.Identity;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Identity
{
    public class RoleService : IRoleService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public RoleService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<IResponseWrapper<string>> CreateAsync(CreateRoleRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.RoleEndpoints.Create, request);
            return await response.WrapToResponse<string>();
        }

        public async Task<IResponseWrapper<string>> DeleteAsync(string roleId)
        {
            var response = await _httpClient.DeleteAsync(_apiSettings.RoleEndpoints.GetDelete(roleId));
            return await response.WrapToResponse<string>();
        }

        public async Task<IResponseWrapper<List<RoleResponse>>> GetRolesAsync()
        {
            var response = await _httpClient.GetAsync(_apiSettings.RoleEndpoints.All);
            return await response.WrapToResponse<List<RoleResponse>>();
        }

        public async Task<IResponseWrapper<RoleResponse>> GetRoleWithoutPermissionsAsync(string roleId)
        {
            var response = await _httpClient.GetAsync(_apiSettings.RoleEndpoints.GetPartial(roleId));
            return await response.WrapToResponse<RoleResponse>();
        }

        public async Task<IResponseWrapper<RoleResponse>> GetRoleWithPermissionsAsync(string roleId)
        {
            var response = await _httpClient.GetAsync(_apiSettings.RoleEndpoints.GetFull(roleId));
            return await response.WrapToResponse<RoleResponse>();
        }

        public async Task<IResponseWrapper<string>> UpdateAsync(UpdateRoleRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.RoleEndpoints.Update, request);
            return await response.WrapToResponse<string>();
        }

        public async Task<IResponseWrapper<string>> UpdatePermissionsAsync(UpdateRolePermissionsRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.RoleEndpoints.UpdatePermissions, request);
            return await response.WrapToResponse<string>();
        }
    }
}
