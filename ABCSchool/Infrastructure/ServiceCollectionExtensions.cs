using Application;
using Application.Features.Chat;
using Application.Features.Identity.Roles;
using Application.Features.Identity.Tokens;
using Application.Features.Identity.Users;
using Application.Features.Schools;
using Application.Features.Tenancy;
using Application.Wrappers;
using Infrastructure.Chat;
using MassTransit;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Infrastructure.Identity.Models;
using Infrastructure.Identity.Tokens;
using Infrastructure.OpenApi;
using Infrastructure.Schools;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NSwag;
using NSwag.Generation.Processors.Security;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .AddDbContext<TenantDbContext>(options => options
                    .UseSqlServer(config.GetConnectionString("DefaultConnection")))
                .AddMultiTenant<TrajanEcoleTenantInfo>()
                    .WithHeaderStrategy(TenancyConstants.TenantIdName)
                    .WithClaimStrategy(TenancyConstants.TenantIdName)
                    .WithEFCoreStore<TenantDbContext, TrajanEcoleTenantInfo>()
                    .Services
                .AddDbContext<ApplicationDbContext>(options => options
                    .UseSqlServer(config.GetConnectionString("DefaultConnection")))
                .AddTransient<ITenantDbSeeder, TenantDbSeeder>()
                .AddTransient<ApplicationDbSeeder>()
                .AddTransient<ITenantService, TenantService>()
                .AddTransient<ISchoolService, SchoolService>()
                .AddChatService(config)
                .AddMessaging(config)
                .AddIdentityService()
                .AddPermissions()
                .AddOpenApiDocumentation(config);
        }

        public static JwtSettings GetJwtSettings(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettingsConfig = config.GetSection(nameof(JwtSettings));
            services.Configure<JwtSettings>(jwtSettingsConfig);

            return jwtSettingsConfig.Get<JwtSettings>();
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services
                .AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(bearer =>
                {
                    bearer.RequireHttpsMetadata = false;
                    bearer.SaveToken = true;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = ClaimTypes.Role,
                        ValidateLifetime = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                    };

                    bearer.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception is SecurityTokenExpiredException)
                            {
                                if (!context.Response.HasStarted)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    var result = JsonConvert.SerializeObject(ResponseWrapper.Fail("Token has expired."));
                                    return context.Response.WriteAsync(result);
                                }

                                return Task.CompletedTask;
                            }

                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                context.Response.ContentType = "application/json";
                                var result = JsonConvert.SerializeObject(ResponseWrapper.Fail("Une erreur inattendue est survenue."));
                                return context.Response.WriteAsync(result);
                            }

                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";
                                var result = JsonConvert.SerializeObject(ResponseWrapper.Fail("Vous n'êtes pas autorisé pour cette opération."));
                                return context.Response.WriteAsync(result);
                            }

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            context.Response.ContentType = "application/json";
                            var result = JsonConvert.SerializeObject(ResponseWrapper.Fail("Vous n'êtes pas autorisé à acceder à cette ressource."));
                            return context.Response.WriteAsync(result);
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                foreach (var prop in typeof(SchoolPermissions).GetNestedTypes()
                    .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
                {
                    var propertyValue = prop.GetValue(null);
                    if (propertyValue is not null)
                    {
                        options.AddPolicy(propertyValue.ToString(), policy => policy
                            .RequireClaim(ClaimConstants.Permission, propertyValue.ToString()));
                    }
                }
            });

            return services;
        }

        internal static IServiceCollection AddChatService(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ChatSettings>(config.GetSection(nameof(ChatSettings)));
            return services.AddTransient<IChatService, ChatService>();
        }



        internal static IServiceCollection AddIdentityService(this IServiceCollection services)
        {
            return services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                }).AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .Services
                .AddScoped<ITokenService, TokenService>()
                .AddScoped<IRoleService, RoleService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<ICurrentUserService, CurrentUserService>()
                .AddScoped<CurrentUserMiddleware>();
        }

        internal static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransit(x =>
            {
                // ABCSchool est uniquement publisher — aucun consumer enregistré ici.
                x.UsingRabbitMq((_, cfg) =>
                {
                    cfg.Host(
                        config["MessageBroker:Host"]        ?? "localhost",
                        config["MessageBroker:VirtualHost"] ?? "/",
                        h =>
                        {
                            h.Username(config["MessageBroker:UserName"] ?? "guest");
                            h.Password(config["MessageBroker:Password"] ?? "guest");
                        });
                });
            });

            return services;
        }

        internal static IServiceCollection AddPermissions(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        }

        internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration config)
        {
            var swaggerSettings = config.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();

            services.AddEndpointsApiExplorer();
            _ = services.AddOpenApiDocument((document, serviceProvider) =>
            {
                document.PostProcess = doc =>
                {
                    doc.Info.Title = swaggerSettings.Title;
                    doc.Info.Description = swaggerSettings.Description;
                    doc.Info.Contact = new OpenApiContact
                    {
                        Name = swaggerSettings.ContactName,
                        Email = swaggerSettings.ContactEmail,
                        Url = swaggerSettings.ContactUrl
                    };
                    doc.Info.License = new OpenApiLicense
                    {
                        Name = swaggerSettings.LicenseName,
                        Url = swaggerSettings.LicenseUrl
                    };
                };

                document.AddSecurity(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter your Bearer token to attach it as a header on your requests.",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });

                document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor());
                document.OperationProcessors.Add(new SwaggerGlobalAuthProcessor());
                document.OperationProcessors.Add(new SwaggerHeaderAttributeProcessor());
            });

            return services;
        }
    }
}
