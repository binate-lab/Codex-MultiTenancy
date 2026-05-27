using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;

namespace Infrastructure
{
    public static class WebApplicationExtensions
    {
        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseMiddleware<CurrentUserMiddleware>();
            app.UseMultiTenant();
            app.UseAuthorization();
            app.UseOpenApiDocumentation();

            return app;
        }

        internal static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
        {
            app.UseOpenApi();
            app.UseSwaggerUi(options =>
            {
                options.DefaultModelExpandDepth = -1;
                options.DocExpansion = "none";
                options.TagsSorter = "alpha";
            });

            return app;
        }
    }
}
