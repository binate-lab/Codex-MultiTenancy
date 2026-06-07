using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.Pki
{
    /// <inheritdoc />
    public partial class InitPki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Pki");

            migrationBuilder.CreateTable(
                name: "CertificatsAppareils",
                schema: "Pki",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UtilisateurId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NomAppareil = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Empreinte = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumeroSerie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmisLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevoqueLe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RaisonRevocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificatsAppareils", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandesCertificats",
                schema: "Pki",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DemandeParAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NomAppareil = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UtilisateurId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DemandeeLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    RaisonRejet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CertificatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesCertificats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificatsAppareils_Empreinte",
                schema: "Pki",
                table: "CertificatsAppareils",
                column: "Empreinte",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificatsAppareils_TenantId_Statut",
                schema: "Pki",
                table: "CertificatsAppareils",
                columns: new[] { "TenantId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandesCertificats_Statut",
                schema: "Pki",
                table: "DemandesCertificats",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesCertificats_TenantId_Statut",
                schema: "Pki",
                table: "DemandesCertificats",
                columns: new[] { "TenantId", "Statut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificatsAppareils",
                schema: "Pki");

            migrationBuilder.DropTable(
                name: "DemandesCertificats",
                schema: "Pki");
        }
    }
}
