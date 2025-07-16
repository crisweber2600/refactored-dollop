#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace ExampleData.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationNameToSaveAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationName",
                table: "SaveAudits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationName",
                table: "SaveAudits");
        }
    }
}
