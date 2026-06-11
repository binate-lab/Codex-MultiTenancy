using Application.Features.Certificats;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Pki
{
    public class CertificatService : ICertificatService
    {
        private readonly PkiDbContext _context;
        private readonly CertificatAutoriteService _ca;

        public CertificatService(PkiDbContext context, IOptions<PkiSettings> settings)
        {
            _context = context;
            _ca = new CertificatAutoriteService(settings);
        }

        public async Task<Guid> SoumettreDemandeAsync(DemandeCertificat demande)
        {
            demande.Id = Guid.NewGuid();
            await _context.DemandesCertificats.AddAsync(demande);
            await _context.SaveChangesAsync();
            return demande.Id;
        }

        public async Task<DemandeCertificat> GetDemandeByIdAsync(Guid demandeId)
            => await _context.DemandesCertificats.FindAsync(demandeId);

        public async Task<List<DemandeCertificat>> GetDemandesByTenantAsync(string tenantId)
            => await _context.DemandesCertificats
                .Where(d => d.TenantId == tenantId)
                .OrderByDescending(d => d.DemandeeLe)
                .ToListAsync();

        public async Task<List<DemandeCertificat>> GetDemandesPendantesAsync()
            => await _context.DemandesCertificats
                .Where(d => d.Statut == StatutDemande.EnAttente)
                .OrderBy(d => d.DemandeeLe)
                .ToListAsync();

        public async Task<CertificatEmisResult> ApprouverDemandeAsync(Guid demandeId, int dureeValiditeJours)
        {
            var demande = await _context.DemandesCertificats.FindAsync(demandeId)
                ?? throw new InvalidOperationException($"Demande {demandeId} introuvable.");

            var genere = _ca.GenererCertificat(demande.NomAppareil, demande.TenantId, dureeValiditeJours);

            var certificat = new CertificatAppareil
            {
                Id = Guid.NewGuid(),
                TenantId = demande.TenantId,
                UtilisateurId = demande.UtilisateurId,
                NomAppareil = demande.NomAppareil,
                Description = demande.Description,
                Empreinte = genere.Empreinte,
                NumeroSerie = genere.NumeroSerie,
                EmisLe = DateTime.UtcNow,
                ExpireLe = genere.ExpireLe,
                Statut = StatutCertificat.Actif
            };

            demande.Statut = StatutDemande.Émise;
            demande.CertificatId = certificat.Id;

            await _context.CertificatsAppareils.AddAsync(certificat);
            await _context.SaveChangesAsync();

            return new CertificatEmisResult
            {
                CertificatId = certificat.Id,
                PfxBase64 = Convert.ToBase64String(genere.Pfx),
                MotDePasse = genere.MotDePasse,
                Empreinte = genere.Empreinte,
                ExpireLe = genere.ExpireLe
            };
        }

        public async Task SupprimerDemandeAsync(Guid demandeId)
        {
            var demande = await _context.DemandesCertificats.FindAsync(demandeId)
                ?? throw new InvalidOperationException($"Demande {demandeId} introuvable.");

            _context.DemandesCertificats.Remove(demande);
            await _context.SaveChangesAsync();
        }

        public async Task RejeterDemandeAsync(Guid demandeId, string raison)
        {
            var demande = await _context.DemandesCertificats.FindAsync(demandeId)
                ?? throw new InvalidOperationException($"Demande {demandeId} introuvable.");

            demande.Statut = StatutDemande.Rejetée;
            demande.RaisonRejet = raison;
            await _context.SaveChangesAsync();
        }

        public async Task<List<CertificatAppareil>> GetCertificatsByTenantAsync(string tenantId)
            => await _context.CertificatsAppareils
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.EmisLe)
                .ToListAsync();

        public async Task<CertificatAppareil> GetCertificatByIdAsync(Guid certificatId)
            => await _context.CertificatsAppareils.FindAsync(certificatId);

        public async Task ReactiverCertificatAsync(Guid certificatId)
        {
            var certificat = await _context.CertificatsAppareils.FindAsync(certificatId)
                ?? throw new InvalidOperationException($"Certificat {certificatId} introuvable.");

            certificat.Statut = StatutCertificat.Actif;
            certificat.RevoqueLe = null;
            certificat.RaisonRevocation = null;
            await _context.SaveChangesAsync();
        }

        public async Task RevoquerCertificatAsync(Guid certificatId, string raison)
        {
            var certificat = await _context.CertificatsAppareils.FindAsync(certificatId)
                ?? throw new InvalidOperationException($"Certificat {certificatId} introuvable.");

            certificat.Statut = StatutCertificat.Révoqué;
            certificat.RevoqueLe = DateTime.UtcNow;
            certificat.RaisonRevocation = raison;
            await _context.SaveChangesAsync();
        }

        public async Task<CertificatAppareil> GetByEmpreinteAsync(string empreinte)
            => await _context.CertificatsAppareils
                .FirstOrDefaultAsync(c => c.Empreinte == empreinte);
    }
}
