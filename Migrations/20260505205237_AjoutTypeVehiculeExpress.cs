using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryWeb.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTypeVehiculeExpress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstExpress",
                table: "Commandes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TypeVehicule",
                table: "Commandes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstExpress",
                table: "Commandes");

            migrationBuilder.DropColumn(
                name: "TypeVehicule",
                table: "Commandes");
        }
    }
}
