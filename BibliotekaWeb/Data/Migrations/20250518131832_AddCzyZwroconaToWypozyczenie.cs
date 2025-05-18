using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotekaWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCzyZwroconaToWypozyczenie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CzyZwrocona",
                table: "Wypozyczenie",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CzyZwrocona",
                table: "Wypozyczenie");
        }
    }
}
