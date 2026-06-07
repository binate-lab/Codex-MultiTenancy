using Domain.Entities;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Contexts
{
    public class ApplicationDbContext : BaseDbContext
    {
        public ApplicationDbContext(
            IMultiTenantContextAccessor<TrajanEcoleTenantInfo> tenantInfoContextAccessor, 
            DbContextOptions<ApplicationDbContext> options) 
            : base(tenantInfoContextAccessor, options)
        {
        }

        public DbSet<School> Schools => Set<School>();
    }
}
