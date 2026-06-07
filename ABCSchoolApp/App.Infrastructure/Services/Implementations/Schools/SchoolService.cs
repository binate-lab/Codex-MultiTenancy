using TrajanEcole.Shared.Library.Models.Requests.Schools;
using TrajanEcole.Shared.Library.Models.Responses.Schools;
using TrajanEcole.Shared.Library.Wrappers;
using App.Infrastructure.Extensions;
using App.Infrastructure.Services.Schools;
using System.Net.Http.Json;

namespace App.Infrastructure.Services.Implementations.Schools
{
    public class SchoolService : ISchoolService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public SchoolService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<IResponseWrapper<int>> CreateAsync(CreateSchoolRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.SchoolEndpoints.Create, request);
            return await response.WrapToResponse<int>();
        }

        public async Task<IResponseWrapper<int>> DeleteAsync(string schoolId)
        {
            var response = await _httpClient.DeleteAsync(_apiSettings.SchoolEndpoints.GetDelete(schoolId));
            return await response.WrapToResponse<int>();
        }

        public async Task<IResponseWrapper<List<SchoolResponse>>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_apiSettings.SchoolEndpoints.All);
            return await response.WrapToResponse<List<SchoolResponse>>();
        }

        public async Task<IResponseWrapper<SchoolResponse>> GetByIdAsync(string schoolId)
        {
            var response = await _httpClient.GetAsync(_apiSettings.SchoolEndpoints.GetById(schoolId));
            return await response.WrapToResponse<SchoolResponse>();
        }

        public async Task<IResponseWrapper<SchoolResponse>> GetByNameAsync(string schoolName)
        {
            var response = await _httpClient.GetAsync(_apiSettings.SchoolEndpoints.GetByName(schoolName));
            return await response.WrapToResponse<SchoolResponse>();
        }

        public async Task<IResponseWrapper<int>> UpdateAsync(UpdateSchoolRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.SchoolEndpoints.Update, request);
            return await response.WrapToResponse<int>();
        }
    }
}
