using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FridayDeploy.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSlowRequestThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlowRequestThresholdMs",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlowRequestThresholdMs",
                table: "AppSettings");
        }
    }
}
