using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FridayDeploy.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionFingerprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExceptionFingerprintId",
                table: "Logs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExceptionFingerprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Application = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SampleMessage = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    SampleStackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    FirstSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionFingerprints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Logs_ExceptionFingerprintId",
                table: "Logs",
                column: "ExceptionFingerprintId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionFingerprints_Fingerprint",
                table: "ExceptionFingerprints",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionFingerprints_LastSeenUtc",
                table: "ExceptionFingerprints",
                column: "LastSeenUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_ExceptionFingerprints_ExceptionFingerprintId",
                table: "Logs",
                column: "ExceptionFingerprintId",
                principalTable: "ExceptionFingerprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // FTS5 virtual table for "related exceptions" search — not mapped by EF Core, created via raw SQL.
            // Fingerprint is UNINDEXED: it's only ever retrieved, never matched against as text.
            migrationBuilder.Sql(
                "CREATE VIRTUAL TABLE IF NOT EXISTS ExceptionSearch USING fts5(Fingerprint UNINDEXED, Content);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS ExceptionSearch;");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_ExceptionFingerprints_ExceptionFingerprintId",
                table: "Logs");

            migrationBuilder.DropTable(
                name: "ExceptionFingerprints");

            migrationBuilder.DropIndex(
                name: "IX_Logs_ExceptionFingerprintId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "ExceptionFingerprintId",
                table: "Logs");
        }
    }
}
