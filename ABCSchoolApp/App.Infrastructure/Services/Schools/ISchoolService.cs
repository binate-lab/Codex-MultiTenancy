using TrajanEcole.Shared.Library.Models.Requests.Schools;
using TrajanEcole.Shared.Library.Models.Responses.Schools;
using TrajanEcole.Shared.Library.Wrappers;

namespace App.Infrastructure.Services.Schools
{
    public interface ISchoolService
    {
        Task<IResponseWrapper<List<SchoolResponse>>> GetAllAsync();
        Task<IResponseWrapper<List<SchoolResponse>>> GetMineAsync();
        Task<IResponseWrapper<int>> CreateAsync(CreateSchoolRequest request);
        Task<IResponseWrapper<int>> UpdateAsync(UpdateSchoolRequest request);
        Task<IResponseWrapper<int>> DeleteAsync(string schoolId); 
        Task<IResponseWrapper<SchoolResponse>> GetByIdAsync(string schoolId);
        Task<IResponseWrapper<SchoolResponse>> GetByNameAsync(string schoolName);
    }
}
