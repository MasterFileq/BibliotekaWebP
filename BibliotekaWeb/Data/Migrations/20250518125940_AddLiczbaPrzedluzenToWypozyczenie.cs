using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotekaWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLiczbaPrzedluzenToWypozyczenie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Wypozyczenie");

            migrationBuilder.AddColumn<int>(
                name: "LiczbaPrzedluzen",
                table: "Wypozyczenie",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiczbaPrzedluzen",
                table: "Wypozyczenie");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Wypozyczenie",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
