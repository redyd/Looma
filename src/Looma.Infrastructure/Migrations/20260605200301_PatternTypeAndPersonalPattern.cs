using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PatternTypeAndPersonalPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PatternType",
                table: "Patterns",
                newName: "IsPersonal");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Patterns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Patterns");

            migrationBuilder.RenameColumn(
                name: "IsPersonal",
                table: "Patterns",
                newName: "PatternType");
        }
    }
}
