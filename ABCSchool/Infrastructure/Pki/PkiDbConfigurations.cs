using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Pki
{
    internal class CertificatAppareilConfig : IEntityTypeConfiguration<CertificatAppareil>
    {
        public void Configure(EntityTypeBuilder<CertificatAppareil> builder)
        {
            builder.ToTable("CertificatsAppareils", "Pki");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.TenantId)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(c => c.NomAppareil)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.Empreinte)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(c => c.Empreinte)
                .IsUnique();

            builder.Property(c => c.NumeroSerie)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.UtilisateurId)
                .HasMaxLength(450);

            builder.Property(c => c.RaisonRevocation)
                .HasMaxLength(500);

            builder.Property(c => c.Statut)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(c => new { c.TenantId, c.Statut });
        }
    }

    internal class DemandeCertificatConfig : IEntityTypeConfiguration<DemandeCertificat>
    {
        public void Configure(EntityTypeBuilder<DemandeCertificat> builder)
        {
            builder.ToTable("DemandesCertificats", "Pki");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.TenantId)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(d => d.DemandeParAdminId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(d => d.NomAppareil)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(d => d.UtilisateurId)
                .HasMaxLength(450);

            builder.Property(d => d.RaisonRejet)
                .HasMaxLength(500);

            builder.Property(d => d.Statut)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(d => new { d.TenantId, d.Statut });
            builder.HasIndex(d => d.Statut);
        }
    }
}
