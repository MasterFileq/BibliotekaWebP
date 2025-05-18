using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotekaWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKsiazkaModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dostepnosc",
                table: "Ksiazka");

            migrationBuilder.AddColumn<int>(
                name: "DostepneEgzemplarze",
                table: "Ksiazka",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IloscEgzemplarzy",
                table: "Ksiazka",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DostepneEgzemplarze",
                table: "Ksiazka");

            migrationBuilder.DropColumn(
                name: "IloscEgzemplarzy",
                table: "Ksiazka");

            migrationBuilder.AddColumn<bool>(
                name: "Dostepnosc",
                table: "Ksiazka",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
