using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FridayDeploy.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAppHourlyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppHourlyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Application = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HourBucketUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WarningCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppHourlyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppHourlyStats_Application_HourBucketUtc",
                table: "AppHourlyStats",
                columns: new[] { "Application", "HourBucketUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppHourlyStats_HourBucketUtc",
                table: "AppHourlyStats",
                column: "HourBucketUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppHourlyStats");
        }
    }
}
