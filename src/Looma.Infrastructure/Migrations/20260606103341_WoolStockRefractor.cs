// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WoolStockRefractor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.RenameColumn(
                name: "LengthToWeightRatio",
                table: "Wools",
                newName: "Weight");

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Wools",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Stock",
                table: "Wools",
                type: "REAL",
                nullable: false,
                defaultValue: 1000.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Length",
                table: "Wools");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Wools");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "Wools",
                newName: "LengthToWeightRatio");

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    StockId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WoolId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightQuantity = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.StockId);
                    table.ForeignKey(
                        name: "FK_Stocks_Wools_WoolId",
                        column: x => x.WoolId,
                        principalTable: "Wools",
                        principalColumn: "WoolId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_WoolId",
                table: "Stocks",
                column: "WoolId");
        }
    }
}
