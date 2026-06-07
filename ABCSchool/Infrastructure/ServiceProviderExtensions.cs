using Infrastructure.Pki;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class ServiceProviderExtensions
    {
        public static async Task AddDatabaseInitializerAsync(this IServiceProvider serviceProvider, CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();

            await scope.ServiceProvider.GetRequiredService<ITenantDbSeeder>()
                .InitializeDatabaseAsync(ct);

            var pkiDb = scope.ServiceProvider.GetRequiredService<PkiDbContext>();
            if ((await pkiDb.Database.GetPendingMigrationsAsync(ct)).Any())
                await pkiDb.Database.MigrateAsync(ct);
        }
    }
}
