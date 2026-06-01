using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddCodeEtsToSchool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeEts",
                schema: "Academics",
                table: "Schools",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_CodeEts",
                schema: "Academics",
                table: "Schools",
                column: "CodeEts",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schools_CodeEts",
                schema: "Academics",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "CodeEts",
                schema: "Academics",
                table: "Schools");
        }
    }
}
