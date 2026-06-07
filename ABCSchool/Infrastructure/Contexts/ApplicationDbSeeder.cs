using Finbuckle.MultiTenant.Abstractions;
using TrajanEcole.Shared.Library.Constants;
//using Infrastructure.Constants;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Contexts
{
    public class ApplicationDbSeeder(
        IMultiTenantContextAccessor<TrajanEcoleTenantInfo> tenantInfoContextAccessor,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext)
    {
        private readonly IMultiTenantContextAccessor<TrajanEcoleTenantInfo> _tenantInfoContextAccessor = tenantInfoContextAccessor;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _applicationDbContext = applicationDbContext;

        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            if (_applicationDbContext.Database.GetMigrations().Any())
            {
                if ((await _applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                {
                    await _applicationDbContext.Database.MigrateAsync(cancellationToken);
                }

                if (await _applicationDbContext.Database.CanConnectAsync(cancellationToken))
                {
                    await InitializeDefaultRolesAsync(cancellationToken);
                    await InitializeAdminUserAsync();
                }
            }
        }

        private async Task InitializeDefaultRolesAsync(CancellationToken ct)
        {
            foreach (var roleName in RoleConstants.DefaultRoles)
            {
                if (await _roleManager.Roles.SingleOrDefaultAsync(role => role.Name == roleName, ct) is not ApplicationRole incomingRole)
                {
                    incomingRole = new ApplicationRole()
                    {
                        Name = roleName,
                        Description = $"{roleName} Role"
                    };

                    await _roleManager.CreateAsync(incomingRole);
                }

                if (roleName == RoleConstants.Admin)
                {
                    // Assign Admin Permissions
                    await AssignPermissionsToRoleAsync(SchoolPermissions.Admin, incomingRole, ct);

                    if (TenancyConstants.IsRoot(_tenantInfoContextAccessor.MultiTenantContext.TenantInfo))
                    {
                        await AssignPermissionsToRoleAsync(SchoolPermissions.Root, incomingRole, ct);
                    }
                }
                else if (roleName == RoleConstants.Basic)
                {
                    // Assign Basic Permissions
                    await AssignPermissionsToRoleAsync(SchoolPermissions.Basic, incomingRole, ct);
                }
            }
        }

        private async Task AssignPermissionsToRoleAsync(
            IReadOnlyList<SchoolPermission> incomingRolePermissions, 
            ApplicationRole currentRole, 
            CancellationToken ct)
        {
            var currentlyAssignedClaims = await _roleManager.GetClaimsAsync(currentRole);

            foreach (var incomingPermission in incomingRolePermissions)
            {
                if (!currentlyAssignedClaims.Any(claim => claim.Type == ClaimConstants.Permission && claim.Value == incomingPermission.Name))
                {
                    await _applicationDbContext.RoleClaims.AddAsync(new ApplicationRoleClaim
                    {
                        RoleId = currentRole.Id,
                        ClaimType = ClaimConstants.Permission,
                        ClaimValue = incomingPermission.Name,
                        Description = incomingPermission.Description,
                        Group = incomingPermission.Group
                    }, ct);

                    await _applicationDbContext.SaveChangesAsync(ct);
                }
            }
        }

        private async Task InitializeAdminUserAsync()
        {
            if (string.IsNullOrEmpty(_tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email)) return;

            if (await _userManager.Users
                .SingleOrDefaultAsync(user => user.Email == _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email)
                is not ApplicationUser incomingUser)
            {
                incomingUser = new ApplicationUser
                {
                    FirstName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.FirstName,
                    LastName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.LastName,
                    Email = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email,
                    UserName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    NormalizedEmail = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email.ToUpperInvariant(),
                    NormalizedUserName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email.ToUpperInvariant(),
                    IsActive = true,
                };

                var passwordHash = new PasswordHasher<ApplicationUser>();

                incomingUser.PasswordHash = passwordHash.HashPassword(incomingUser, TenancyConstants.DefaultPassword);
                await _userManager.CreateAsync(incomingUser);
            }

            if (!await _userManager.IsInRoleAsync(incomingUser, RoleConstants.Admin))
            {
                await _userManager.AddToRoleAsync(incomingUser, RoleConstants.Admin);
            }
        }
    }
}
