using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerocheNow.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAccesInstagram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccesInstagram",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Jeton = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Compte = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObtenuLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpireLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccesInstagram", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccesInstagram");
        }
    }
}
