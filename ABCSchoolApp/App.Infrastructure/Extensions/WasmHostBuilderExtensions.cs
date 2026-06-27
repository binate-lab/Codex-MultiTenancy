using TrajanEcole.Shared.Library.Constants;
using App.Infrastructure.Services.Auth;
using App.Infrastructure.Services.Identity;
using App.Infrastructure.Services.Implementations.Identity;
using App.Infrastructure.Services.Implementations.Interceptors;
using App.Infrastructure.Services.Certificats;
using App.Infrastructure.Services.Chat;
using App.Infrastructure.Services.Implementations.Certificats;
using App.Infrastructure.Services.Implementations.Chat;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Implementations.Eleves;
using App.Infrastructure.Services.AnneesScolaires;
using App.Infrastructure.Services.Implementations.AnneesScolaires;
using App.Infrastructure.Services.Implementations.Schools;
using App.Infrastructure.Services.Implementations.Tenancy;
using App.Infrastructure.Services.Interceptors;
using App.Infrastructure.Services.Schools;
using App.Infrastructure.Services.Tenancy;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace App.Infrastructure.Extensions
{
    public static class WasmHostBuilderExtensions
    {
        private const string _clientName = "ABC School Api";
        public static WebAssemblyHostBuilder AddClientServices(this WebAssemblyHostBuilder builder)
        {
            builder.Services
                .AddAuthorizationCore(RegisterPermissions)
                .AddBlazoredLocalStorage()
                .AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
                    config.SnackbarConfiguration.HideTransitionDuration = 100;
                    config.SnackbarConfiguration.ShowTransitionDuration = 100;
                    config.SnackbarConfiguration.VisibleStateDuration = 5000;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                })
                .AddScoped<ApplicationStateProvider>()
                .AddScoped<AuthenticationStateProvider, ApplicationStateProvider>()
                .AddTransient<AuthenticationHeaderHandler>()
                .AddScoped<ITokenService, TokenService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<ITenantService, TenantService>()
                .AddScoped<IRoleService, RoleService>()
                .AddScoped<ISchoolService, SchoolService>()
                .AddScoped<IAnneeScolaireService, AnneeScolaireService>()
                .AddScoped<ICertificatService, CertificatService>()
                .AddScoped<IChatService, ChatService>()
                .AddScoped<IHttpRefreshTokenInterceptorService, HttpRefreshTokenInterceptorService>()
                .AddScoped(sp => sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(_clientName).EnableIntercept(sp))
                .AddHttpClient(_clientName, client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration.GetSection("ApiSettings:BaseApiUrl").Get<string>());
                })
                .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            // Client typie dedie au microservice Eleves.Api (base address propre + JWT propage).
            builder.Services
                .AddHttpClient<IEleveService, EleveService>(client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration.GetSection("ApiSettings:ElevesApiUrl").Get<string>());
                })
                .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            // #5 : client typie dedie au microservice Scolarite.Api (compteur N° Inscription par ecole).
            builder.Services
                .AddHttpClient<IInscriptionService, InscriptionService>(client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration.GetSection("ApiSettings:ScolariteApiUrl").Get<string>());
                })
                .AddHttpMessageHandler<AuthenticationHeaderHandler>();

            builder.Services.AddHttpClientInterceptor();

            return builder;
        }

        private static void RegisterPermissions(AuthorizationOptions options)
        {
            foreach (var permission in SchoolPermissions.All)
            {
                options.AddPolicy(permission.Name, policy => policy.RequireClaim(ClaimConstants.Permission, permission.Name));
            }
        }
    }
}
