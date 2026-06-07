using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Pki
{
    public class PkiDbContext(DbContextOptions<PkiDbContext> options) : DbContext(options)
    {
        public DbSet<CertificatAppareil> CertificatsAppareils => Set<CertificatAppareil>();
        public DbSet<DemandeCertificat> DemandesCertificats => Set<DemandeCertificat>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new CertificatAppareilConfig());
            builder.ApplyConfiguration(new DemandeCertificatConfig());
        }
    }
}
