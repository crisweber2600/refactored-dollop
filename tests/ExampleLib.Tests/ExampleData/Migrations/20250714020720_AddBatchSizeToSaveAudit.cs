using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExampleData.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchSizeToSaveAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchSize",
                table: "SaveAudits",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchSize",
                table: "SaveAudits");
        }
    }
}
