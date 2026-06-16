// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImproveProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Patterns_PatronId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "PatronId",
                table: "Projects",
                newName: "PatternId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_PatronId",
                table: "Projects",
                newName: "IX_Projects_PatternId");

            migrationBuilder.AddColumn<double>(
                name: "StockUsed",
                table: "WoolsForProjects",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Patterns_PatternId",
                table: "Projects",
                column: "PatternId",
                principalTable: "Patterns",
                principalColumn: "PatternId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Patterns_PatternId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StockUsed",
                table: "WoolsForProjects");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "PatternId",
                table: "Projects",
                newName: "PatronId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_PatternId",
                table: "Projects",
                newName: "IX_Projects_PatronId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Patterns_PatronId",
                table: "Projects",
                column: "PatronId",
                principalTable: "Patterns",
                principalColumn: "PatternId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
