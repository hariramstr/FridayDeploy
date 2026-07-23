using FridayDeploy.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FridayDeploy.Web.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260723000000_InitialCreate")]
public sealed partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Logs",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Application = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Environment = table.Column<string>(type: "TEXT", nullable: true),
                Version = table.Column<string>(type: "TEXT", nullable: true),
                Machine = table.Column<string>(type: "TEXT", nullable: true),
                Level = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                Exception = table.Column<string>(type: "TEXT", nullable: true),
                StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                Source = table.Column<string>(type: "TEXT", nullable: true),
                RequestId = table.Column<string>(type: "TEXT", nullable: true),
                CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                ThreadId = table.Column<string>(type: "TEXT", nullable: true),
                Duration = table.Column<double>(type: "REAL", nullable: true),
                IPAddress = table.Column<string>(type: "TEXT", nullable: true),
                PropertiesJson = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Logs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Username = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                IsAdministrator = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LogProperties",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                LogId = table.Column<long>(type: "INTEGER", nullable: false),
                PropertyName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                PropertyValue = table.Column<string>(type: "TEXT", nullable: true),
                PropertyType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LogProperties", x => x.Id);
                table.ForeignKey(
                    name: "FK_LogProperties_Logs_LogId",
                    column: x => x.LogId,
                    principalTable: "Logs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_LogProperties_LogId", "LogProperties", "LogId");
        migrationBuilder.CreateIndex("IX_LogProperties_PropertyName_PropertyValue", "LogProperties", new[] { "PropertyName", "PropertyValue" });
        migrationBuilder.CreateIndex("IX_Logs_Application", "Logs", "Application");
        migrationBuilder.CreateIndex("IX_Logs_Level", "Logs", "Level");
        migrationBuilder.CreateIndex("IX_Logs_TimestampUtc", "Logs", "TimestampUtc");
        migrationBuilder.CreateIndex("IX_Users_Username", "Users", "Username", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("LogProperties");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropTable("Logs");
    }
}
