using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerocheNow.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Passe le schéma en anglais, comme le code : tables, colonnes, clés, index et valeurs de rubrique.
    /// Écrite à la main pour renommer sans perte de données (EF proposait de supprimer puis recréer les tables).
    /// Les traces d'audit déjà écrites gardent leur action et leur auteur d'origine.
    /// </summary>
    public partial class RenameSchemaToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Les contraintes portent le nom des anciennes tables : on les retire avant de renommer.
            migrationBuilder.DropForeignKey(
                name: "FK_LigneDuMoment_StatutDuMoment_StatutDuMomentId",
                table: "LigneDuMoment");

            migrationBuilder.DropPrimaryKey(name: "PK_LigneDuMoment", table: "LigneDuMoment");
            migrationBuilder.DropPrimaryKey(name: "PK_StatutDuMoment", table: "StatutDuMoment");
            migrationBuilder.DropPrimaryKey(name: "PK_TraceAudit", table: "TraceAudit");

            migrationBuilder.RenameTable(name: "StatutDuMoment", newName: "CurrentStatus");
            migrationBuilder.RenameTable(name: "LigneDuMoment", newName: "StatusLine");
            migrationBuilder.RenameTable(name: "TraceAudit", newName: "AuditEntry");

            migrationBuilder.RenameColumn(name: "Accroche", table: "CurrentStatus", newName: "Headline");
            migrationBuilder.RenameColumn(name: "ObjectifLibelle", table: "CurrentStatus", newName: "GoalLabel");
            migrationBuilder.RenameColumn(name: "ObjectifDateCible", table: "CurrentStatus", newName: "GoalTargetDate");
            migrationBuilder.RenameColumn(name: "MisAJourLe", table: "CurrentStatus", newName: "UpdatedAt");

            migrationBuilder.RenameColumn(name: "Ordre", table: "StatusLine", newName: "Position");
            migrationBuilder.RenameColumn(name: "Texte", table: "StatusLine", newName: "Text");
            migrationBuilder.RenameColumn(name: "Rubrique", table: "StatusLine", newName: "Section");
            migrationBuilder.RenameColumn(name: "StatutDuMomentId", table: "StatusLine", newName: "CurrentStatusId");

            migrationBuilder.RenameColumn(name: "Auteur", table: "AuditEntry", newName: "Author");
            migrationBuilder.RenameColumn(name: "TypeObjet", table: "AuditEntry", newName: "ObjectType");
            migrationBuilder.RenameColumn(name: "IdObjet", table: "AuditEntry", newName: "ObjectId");
            migrationBuilder.RenameColumn(name: "Le", table: "AuditEntry", newName: "OccurredAt");

            migrationBuilder.RenameIndex(name: "IX_LigneDuMoment_StatutDuMomentId", table: "StatusLine", newName: "IX_StatusLine_CurrentStatusId");
            migrationBuilder.RenameIndex(name: "IX_TraceAudit_IdObjet", table: "AuditEntry", newName: "IX_AuditEntry_ObjectId");
            migrationBuilder.RenameIndex(name: "IX_TraceAudit_Le", table: "AuditEntry", newName: "IX_AuditEntry_OccurredAt");

            migrationBuilder.AddPrimaryKey(name: "PK_CurrentStatus", table: "CurrentStatus", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_StatusLine", table: "StatusLine", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_AuditEntry", table: "AuditEntry", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusLine_CurrentStatus_CurrentStatusId",
                table: "StatusLine",
                column: "CurrentStatusId",
                principalTable: "CurrentStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Les rubriques sont stockées en clair : on traduit les valeurs existantes.
            migrationBuilder.Sql("""
                UPDATE [StatusLine] SET [Section] = CASE [Section]
                    WHEN N'Aucune' THEN N'None'
                    WHEN N'ProjetEnCours' THEN N'CurrentProject'
                    WHEN N'Contenus' THEN N'Content'
                    WHEN N'HorsPiste' THEN N'OffTrack'
                    ELSE [Section] END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [StatusLine] SET [Section] = CASE [Section]
                    WHEN N'None' THEN N'Aucune'
                    WHEN N'CurrentProject' THEN N'ProjetEnCours'
                    WHEN N'Content' THEN N'Contenus'
                    WHEN N'OffTrack' THEN N'HorsPiste'
                    ELSE [Section] END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_StatusLine_CurrentStatus_CurrentStatusId",
                table: "StatusLine");

            migrationBuilder.DropPrimaryKey(name: "PK_StatusLine", table: "StatusLine");
            migrationBuilder.DropPrimaryKey(name: "PK_CurrentStatus", table: "CurrentStatus");
            migrationBuilder.DropPrimaryKey(name: "PK_AuditEntry", table: "AuditEntry");

            migrationBuilder.RenameIndex(name: "IX_StatusLine_CurrentStatusId", table: "StatusLine", newName: "IX_LigneDuMoment_StatutDuMomentId");
            migrationBuilder.RenameIndex(name: "IX_AuditEntry_ObjectId", table: "AuditEntry", newName: "IX_TraceAudit_IdObjet");
            migrationBuilder.RenameIndex(name: "IX_AuditEntry_OccurredAt", table: "AuditEntry", newName: "IX_TraceAudit_Le");

            migrationBuilder.RenameColumn(name: "Author", table: "AuditEntry", newName: "Auteur");
            migrationBuilder.RenameColumn(name: "ObjectType", table: "AuditEntry", newName: "TypeObjet");
            migrationBuilder.RenameColumn(name: "ObjectId", table: "AuditEntry", newName: "IdObjet");
            migrationBuilder.RenameColumn(name: "OccurredAt", table: "AuditEntry", newName: "Le");

            migrationBuilder.RenameColumn(name: "Position", table: "StatusLine", newName: "Ordre");
            migrationBuilder.RenameColumn(name: "Text", table: "StatusLine", newName: "Texte");
            migrationBuilder.RenameColumn(name: "Section", table: "StatusLine", newName: "Rubrique");
            migrationBuilder.RenameColumn(name: "CurrentStatusId", table: "StatusLine", newName: "StatutDuMomentId");

            migrationBuilder.RenameColumn(name: "Headline", table: "CurrentStatus", newName: "Accroche");
            migrationBuilder.RenameColumn(name: "GoalLabel", table: "CurrentStatus", newName: "ObjectifLibelle");
            migrationBuilder.RenameColumn(name: "GoalTargetDate", table: "CurrentStatus", newName: "ObjectifDateCible");
            migrationBuilder.RenameColumn(name: "UpdatedAt", table: "CurrentStatus", newName: "MisAJourLe");

            migrationBuilder.RenameTable(name: "CurrentStatus", newName: "StatutDuMoment");
            migrationBuilder.RenameTable(name: "StatusLine", newName: "LigneDuMoment");
            migrationBuilder.RenameTable(name: "AuditEntry", newName: "TraceAudit");

            migrationBuilder.AddPrimaryKey(name: "PK_StatutDuMoment", table: "StatutDuMoment", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_LigneDuMoment", table: "LigneDuMoment", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_TraceAudit", table: "TraceAudit", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LigneDuMoment_StatutDuMoment_StatutDuMomentId",
                table: "LigneDuMoment",
                column: "StatutDuMomentId",
                principalTable: "StatutDuMoment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
