using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryWeb.Migrations
{
    /// <inheritdoc />
    public partial class AjoutRefusLivraison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefusLivraisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandeId = table.Column<int>(type: "int", nullable: false),
                    LivreurId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateRefus = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefusLivraisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefusLivraisons_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefusLivraisons_CommandeId",
                table: "RefusLivraisons",
                column: "CommandeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefusLivraisons");
        }
    }
}
