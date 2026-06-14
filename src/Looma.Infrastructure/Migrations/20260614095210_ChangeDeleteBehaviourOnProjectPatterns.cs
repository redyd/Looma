using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDeleteBehaviourOnProjectPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Patterns_PatternId",
                table: "Projects");

            migrationBuilder.AlterColumn<int>(
                name: "PatternId",
                table: "Projects",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Patterns_PatternId",
                table: "Projects",
                column: "PatternId",
                principalTable: "Patterns",
                principalColumn: "PatternId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Patterns_PatternId",
                table: "Projects");

            migrationBuilder.AlterColumn<int>(
                name: "PatternId",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Patterns_PatternId",
                table: "Projects",
                column: "PatternId",
                principalTable: "Patterns",
                principalColumn: "PatternId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
