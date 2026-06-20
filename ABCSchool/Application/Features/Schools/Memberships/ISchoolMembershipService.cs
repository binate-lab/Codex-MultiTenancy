using Domain.Entities;

namespace Application.Features.Schools.Memberships
{
    public interface ISchoolMembershipService
    {
        Task<int> AssignAsync(string userId, int schoolId, string roleId);
        Task RevokeAsync(string userId, int schoolId, string roleId);
        Task<List<School>> GetUserSchoolsAsync(string userId);
        Task<List<SchoolMembership>> GetUserMembershipsAsync(string userId);
        Task<List<string>> GetUserRoleIdsInSchoolAsync(string userId, int schoolId);
        Task<bool> IsMemberAsync(string userId, int schoolId);
    }
}
