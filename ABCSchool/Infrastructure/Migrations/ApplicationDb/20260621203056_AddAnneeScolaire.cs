using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddAnneeScolaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnneesScolaires",
                schema: "Academics",
                columns: table => new
                {
                    AnneeScolaire = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DebutAnneeScolaire = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinAnneeScolaire = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinSemestre1 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinSemestre2 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinTrimestre1 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinTrimestre2 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnneeEnCours = table.Column<bool>(type: "bit", nullable: false),
                    DelaiExclusion = table.Column<int>(type: "int", nullable: false),
                    FinEncaissement = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnneesScolaires", x => x.AnneeScolaire);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnneesScolaires",
                schema: "Academics");
        }
    }
}
