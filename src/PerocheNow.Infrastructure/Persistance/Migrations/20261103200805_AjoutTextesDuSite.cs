using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerocheNow.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTextesDuSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TexteDuSite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valeur = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ModifieLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TexteDuSite", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TexteDuSite_Cle",
                table: "TexteDuSite",
                column: "Cle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TexteDuSite");
        }
    }
}
