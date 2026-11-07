using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerocheNow.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMediasDuSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaDuSite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fichier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TypeContenu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Taille = table.Column<long>(type: "bigint", nullable: false),
                    ModifieLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaDuSite", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaDuSite_Cle",
                table: "MediaDuSite",
                column: "Cle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaDuSite");
        }
    }
}
