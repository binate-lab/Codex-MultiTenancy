using Application;
using Infrastructure;
using Infrastructure.Pki;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using WebApi;

const string BlazorClientCorsPolicy = "BlazorClient";

var builder = WebApplication.CreateBuilder(args);

// Demander un certificat client uniquement si la validation PKI est activée.
// En dev (ValiderCertificatAppareil: false) → pas de boîte de dialogue dans le navigateur.
var pkiSettings = builder.Configuration.GetSection("PkiSettings").Get<PkiSettings>();

builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureHttpsDefaults(https =>
    {
        if (pkiSettings?.ValiderCertificatAppareil == true)
        {
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.AllowAnyClientCertificate();
        }
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

// Sert les fichiers statiques (dont les dossiers par école créés sous wwwroot/{NomCourtEts}).
app.UseStaticFiles();

app.UseInfrastructure();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.Run();
