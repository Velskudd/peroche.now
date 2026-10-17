using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerocheNow.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class InitialAvecRubriques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatutDuMoment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Accroche = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    ObjectifLibelle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ObjectifDateCible = table.Column<DateOnly>(type: "date", nullable: true),
                    MisAJourLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutDuMoment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TraceAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Auteur = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TypeObjet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdObjet = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Le = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LigneDuMoment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Texte = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rubrique = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutDuMomentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneDuMoment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LigneDuMoment_StatutDuMoment_StatutDuMomentId",
                        column: x => x.StatutDuMomentId,
                        principalTable: "StatutDuMoment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LigneDuMoment_StatutDuMomentId",
                table: "LigneDuMoment",
                column: "StatutDuMomentId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAudit_IdObjet",
                table: "TraceAudit",
                column: "IdObjet");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAudit_Le",
                table: "TraceAudit",
                column: "Le");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LigneDuMoment");

            migrationBuilder.DropTable(
                name: "TraceAudit");

            migrationBuilder.DropTable(
                name: "StatutDuMoment");
        }
    }
}
