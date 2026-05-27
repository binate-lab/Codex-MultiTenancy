using Infrastructure.Tenancy;
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
        }
    }
}
