using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenancy
{
    public class TenantDbSeeder : ITenantDbSeeder
    {
        private readonly TenantDbContext _tenantDbContext;
        private readonly IServiceProvider _serviceProvider;

        public TenantDbSeeder(TenantDbContext tenantDbContext, IServiceProvider serviceProvider)
        {
            _tenantDbContext = tenantDbContext;
            _serviceProvider = serviceProvider;
        }


        public async Task InitializeDatabaseAsync(CancellationToken ct)
        {
            await InitializeDatabaseWithTenantAsync(ct);

            foreach (var tenant in await _tenantDbContext.TenantInfo.ToListAsync(ct))
            {
                await InitializeApplicationDbForTenantAsync(tenant, ct);
            }
        }

        private async Task InitializeDatabaseWithTenantAsync(CancellationToken ct)
        {
            if (_tenantDbContext.Database.GetMigrations().Any())
            {
                if ((await _tenantDbContext.Database.GetPendingMigrationsAsync(ct)).Any())
                {
                    await _tenantDbContext.Database.MigrateAsync(ct);
                }
            }

            if (await _tenantDbContext.TenantInfo
                    .SingleOrDefaultAsync(tenant => tenant.Identifier == TenancyConstants.Root.Identifier, ct)
                is null)
            {
                // Create tenant
                var rootTenant = new ABCSchoolTenantInfo
                {
                    Id = TenancyConstants.Root.Id,
                    Identifier = TenancyConstants.Root.Identifier,
                    Name = TenancyConstants.Root.Name,
                    Email = TenancyConstants.Root.Email,
                    FirstName = TenancyConstants.FirstName,
                    LastName = TenancyConstants.LastName,
                    IsActive = true,
                    ValidUpTo = DateTime.UtcNow.AddYears(2)
                };

                await _tenantDbContext.TenantInfo.AddAsync(rootTenant, ct);
                await _tenantDbContext.SaveChangesAsync(ct);
            } 
        }

        private async Task InitializeApplicationDbForTenantAsync(ABCSchoolTenantInfo currentTenant,  CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<ABCSchoolTenantInfo>(currentTenant);

            await scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>()
                .InitializeDatabaseAsync(ct);
        }
    }
}
