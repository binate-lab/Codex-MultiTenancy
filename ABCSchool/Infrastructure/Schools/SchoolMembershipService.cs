using Application.Features.Schools.Memberships;
using Domain.Entities;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Schools
{
    public class SchoolMembershipService : ISchoolMembershipService
    {
        private readonly ApplicationDbContext _context;

        public SchoolMembershipService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> AssignAsync(string userId, int schoolId, string roleId)
        {
            var existing = await _context.SchoolMemberships
                .FirstOrDefaultAsync(membership =>
                    membership.UserId == userId &&
                    membership.SchoolId == schoolId &&
                    membership.RoleId == roleId);

            if (existing is not null)
            {
                return existing.Id;
            }

            var newMembership = new SchoolMembership
            {
                UserId = userId,
                SchoolId = schoolId,
                RoleId = roleId,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await _context.SchoolMemberships.AddAsync(newMembership);
            await _context.SaveChangesAsync();

            return newMembership.Id;
        }

        public async Task RevokeAsync(string userId, int schoolId, string roleId)
        {
            var membership = await _context.SchoolMemberships
                .FirstOrDefaultAsync(membership =>
                    membership.UserId == userId &&
                    membership.SchoolId == schoolId &&
                    membership.RoleId == roleId);

            if (membership is not null)
            {
                _context.SchoolMemberships.Remove(membership);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<School>> GetUserSchoolsAsync(string userId)
        {
            return await _context.SchoolMemberships
                .Where(membership => membership.UserId == userId && membership.IsActive)
                .Select(membership => membership.School)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<SchoolMembership>> GetUserMembershipsAsync(string userId)
        {
            return await _context.SchoolMemberships
                .Include(membership => membership.School)
                .Where(membership => membership.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserRoleIdsInSchoolAsync(string userId, int schoolId)
        {
            return await _context.SchoolMemberships
                .Where(membership =>
                    membership.UserId == userId &&
                    membership.SchoolId == schoolId &&
                    membership.IsActive)
                .Select(membership => membership.RoleId)
                .ToListAsync();
        }

        public async Task<bool> IsMemberAsync(string userId, int schoolId)
        {
            return await _context.SchoolMemberships
                .AnyAsync(membership =>
                    membership.UserId == userId &&
                    membership.SchoolId == schoolId &&
                    membership.IsActive);
        }
    }
}
