using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.Pki
{
    internal class CertificatGenereResult
    {
        public byte[] Pfx { get; set; }
        public string MotDePasse { get; set; }
        public string Empreinte { get; set; }
        public string NumeroSerie { get; set; }
        public DateTime ExpireLe { get; set; }
    }

    internal class CertificatAutoriteService
    {
        private readonly PkiSettings _settings;

        public CertificatAutoriteService(IOptions<PkiSettings> settings)
        {
            _settings = settings.Value;
        }

        public CertificatGenereResult GenererCertificat(string nomAppareil, string tenantId, int dureeJours)
        {
            var caCert = ChargerCertificatCA();

            using var rsa = RSA.Create(2048);

            var sujet = new X500DistinguishedName(
                $"CN={nomAppareil}, O=ABCSchool, OU={tenantId}");

            var req = new CertificateRequest(sujet, rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

            req.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

            var numeroSerie = new byte[8];
            RandomNumberGenerator.Fill(numeroSerie);

            var notBefore = DateTimeOffset.UtcNow;
            var notAfter = notBefore.AddDays(dureeJours);

            var cert = req.Create(caCert, notBefore, notAfter, numeroSerie);
            var certAvecCle = cert.CopyWithPrivateKey(rsa);

            var motDePasse = GenererMotDePasse();
            var pfx = certAvecCle.Export(X509ContentType.Pfx, motDePasse);

            return new CertificatGenereResult
            {
                Pfx = pfx,
                MotDePasse = motDePasse,
                Empreinte = cert.Thumbprint,
                NumeroSerie = cert.SerialNumber,
                ExpireLe = notAfter.UtcDateTime
            };
        }

        private X509Certificate2 ChargerCertificatCA()
        {
            if (!File.Exists(_settings.CaCertPath))
                throw new FileNotFoundException(
                    $"Certificat CA introuvable : {_settings.CaCertPath}. " +
                    "Générez une CA racine et configurez PkiSettings:CaCertPath.");

            return X509CertificateLoader.LoadPkcs12FromFile(
                _settings.CaCertPath,
                _settings.CaCertPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        private static string GenererMotDePasse()
        {
            var bytes = new byte[18];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
