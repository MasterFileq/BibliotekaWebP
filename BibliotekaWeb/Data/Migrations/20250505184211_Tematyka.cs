using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotekaWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class Tematyka : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tematyka",
                table: "Ksiazka",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tematyka",
                table: "Ksiazka");
        }
    }
}
