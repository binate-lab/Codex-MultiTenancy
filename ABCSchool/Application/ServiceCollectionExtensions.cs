using Application.Pipelines;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = typeof(ValidationPipelineBenaviour<,>).Assembly;

            return services
                .AddValidatorsFromAssembly(assembly)
                .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBenaviour<,>))
                .AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(assembly);
                });
        }
    }
}
