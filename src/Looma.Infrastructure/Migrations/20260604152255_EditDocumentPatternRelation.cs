// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditDocumentPatternRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentPattern");

            migrationBuilder.AddColumn<int>(
                name: "PatternId",
                table: "Documents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PatternId",
                table: "Documents",
                column: "PatternId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Patterns_PatternId",
                table: "Documents",
                column: "PatternId",
                principalTable: "Patterns",
                principalColumn: "PatternId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Patterns_PatternId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_PatternId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PatternId",
                table: "Documents");

            migrationBuilder.CreateTable(
                name: "DocumentPattern",
                columns: table => new
                {
                    DocumentsDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatternsPatternId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPattern", x => new { x.DocumentsDocumentId, x.PatternsPatternId });
                    table.ForeignKey(
                        name: "FK_DocumentPattern_Documents_DocumentsDocumentId",
                        column: x => x.DocumentsDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentPattern_Patterns_PatternsPatternId",
                        column: x => x.PatternsPatternId,
                        principalTable: "Patterns",
                        principalColumn: "PatternId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPattern_PatternsPatternId",
                table: "DocumentPattern",
                column: "PatternsPatternId");
        }
    }
}
