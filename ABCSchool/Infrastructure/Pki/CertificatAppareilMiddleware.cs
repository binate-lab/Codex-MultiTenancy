using Application.Wrappers;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Infrastructure.Pki
{
    public class CertificatAppareilMiddleware : IMiddleware
    {
        private readonly PkiDbContext _pkiContext;
        private readonly PkiSettings _settings;

        private static readonly string[] _cheminsExempts =
        [
            "/swagger",
            "/api-docs",
            "/openapi",
            "/health"
        ];

        public CertificatAppareilMiddleware(PkiDbContext pkiContext, IOptions<PkiSettings> settings)
        {
            _pkiContext = pkiContext;
            _settings = settings.Value;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!_settings.ValiderCertificatAppareil || EstExempt(context.Request.Path))
            {
                await next(context);
                return;
            }

            var cert = ObtenirCertificat(context);

            if (cert is null)
            {
                await RefuserAsync(context, 403, "Appareil non reconnu. Un certificat valide est requis pour accéder à cette application.");
                return;
            }

            var certificat = await _pkiContext.CertificatsAppareils
                .FirstOrDefaultAsync(c => c.Empreinte == cert.Thumbprint);

            if (certificat is null)
            {
                await RefuserAsync(context, 403, "Appareil non autorisé. Contactez votre administrateur.");
                return;
            }

            // Mise à jour automatique du statut si expiré
            if (certificat.Statut == StatutCertificat.Actif && certificat.ExpireLe < DateTime.UtcNow)
            {
                certificat.Statut = StatutCertificat.Expiré;
                await _pkiContext.SaveChangesAsync();
            }

            if (certificat.Statut != StatutCertificat.Actif)
            {
                var raison = certificat.Statut == StatutCertificat.Révoqué
                    ? "révoqué — cet appareil a été désactivé"
                    : "expiré — contactez votre administrateur pour un renouvellement";

                await RefuserAsync(context, 403, $"Accès refusé : certificat {raison}.");
                return;
            }

            await next(context);
        }

        private static X509Certificate2 ObtenirCertificat(HttpContext context)
        {
            // Certificat direct Kestrel (mTLS)
            if (context.Connection.ClientCertificate is not null)
                return context.Connection.ClientCertificate;

            // Certificat transmis par reverse proxy (nginx / IIS) en header PEM
            if (context.Request.Headers.TryGetValue("X-Client-Cert", out var pemHeader) &&
                !string.IsNullOrEmpty(pemHeader))
            {
                try
                {
                    var pem = Uri.UnescapeDataString(pemHeader.ToString());
                    return X509CertificateLoader.LoadCertificate(
                        System.Text.Encoding.UTF8.GetBytes(pem));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static bool EstExempt(PathString path)
        {
            foreach (var chemin in _cheminsExempts)
            {
                if (path.StartsWithSegments(chemin, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static async Task RefuserAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(
                ResponseWrapper.Fail(message),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await context.Response.WriteAsync(body);
        }
    }
}
