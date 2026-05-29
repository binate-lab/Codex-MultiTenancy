using Application.Features.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Contexts;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenancy
{
    public class TenantService : ITenantService
    {
        private readonly IMultiTenantStore<ABCSchoolTenantInfo> _tenantStore;
        private readonly ApplicationDbSeeder _dbSeeder;
        private readonly IServiceProvider _serviceProvider;

        public TenantService(IMultiTenantStore<ABCSchoolTenantInfo> tenantStore, ApplicationDbSeeder dbSeeder, IServiceProvider serviceProvider)
        {
            _tenantStore = tenantStore;
            _dbSeeder = dbSeeder;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> ActivateAsync(string identifier)
        {
            var tenantInDb = await _tenantStore.GetByIdentifierAsync(identifier);
            tenantInDb.IsActive = true;

            await _tenantStore.UpdateAsync(tenantInDb);
            return tenantInDb.Identifier;
        }

        public async Task<string> CreateTenantAsync(CreateTenantRequest createTenant, CancellationToken ct)
        {
            var newTenant = new ABCSchoolTenantInfo
            {
                Id = Guid.NewGuid().ToString(),
                Identifier = createTenant.Identifier,
                Name = createTenant.Name,
                IsActive = createTenant.IsActive,
                ConnectionString = createTenant.ConnectionString,
                Email = createTenant.Email,
                FirstName = createTenant.FirstName,
                LastName = createTenant.LastName,
                ValidUpTo = createTenant.ValidUpTo
            };

            await _tenantStore.AddAsync(newTenant);

            // Seeding tenant data
            using var scope = _serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<ABCSchoolTenantInfo>(newTenant);
            await scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>()
                .InitializeDatabaseAsync(ct);

            return newTenant.Identifier;
        }

        public async Task<string> DeactivateAsync(string identifier)
        {
            var tenantInDb = await _tenantStore.GetByIdentifierAsync(identifier);
            tenantInDb.IsActive = false;

            await _tenantStore.UpdateAsync(tenantInDb);
            return tenantInDb.Identifier;
        }

        public async Task<TenantResponse> GetTenantByIdentifierAsync(string identifier)
        {
            var tenantInDb = await _tenantStore.GetByIdentifierAsync(identifier);

            #region Manual Mapping
            //var tenantResponse = new TenantResponse
            //{
            //    Identifier = tenantInDb.Identifier,
            //    Name = tenantInDb.Name,
            //    ConnectionString = tenantInDb.ConnectionString,
            //    Email = tenantInDb.Email,
            //    FirstName = tenantInDb.FirstName,
            //    LastName = tenantInDb.LastName,
            //    IsActive = tenantInDb.IsActive,
            //    ValidUpTo = tenantInDb.ValidUpTo
            //};
            //return tenantResponse;
            #endregion
            // Mapster
            return tenantInDb.Adapt<TenantResponse>();

        }

        public async Task<List<TenantResponse>> GetTenantsAsync()
        {
            var tenantsInDb = await _tenantStore.GetAllAsync();
            return tenantsInDb.Adapt<List<TenantResponse>>();
        }

        public async Task<string> UpdateSubscriptionAsync(UpdateTenantSubscriptionRequest updateTenantSubscription)
        {
            var tenantInDb = await _tenantStore.GetByIdentifierAsync(updateTenantSubscription.TenantIdentifier);

            tenantInDb.ValidUpTo = updateTenantSubscription.NewExpiryDate;

            await _tenantStore.UpdateAsync(tenantInDb);

            return tenantInDb.Identifier;
        }
    }
}
