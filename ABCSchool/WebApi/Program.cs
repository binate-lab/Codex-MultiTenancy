using Application;
using Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using WebApi;

const string BlazorClientCorsPolicy = "BlazorClient";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureHttpsDefaults(https =>
    {
        https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        https.AllowAnyClientCertificate();
    }));

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(BlazorClientCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Services.GetJwtSettings(builder.Configuration));

builder.Services.AddApplicationServices();

var app = builder.Build();

await app.Services.AddDatabaseInitializerAsync();

app.UseCors(BlazorClientCorsPolicy);

app.UseHttpsRedirection();

app.UseInfrastructure();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.Run();
