// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrackedWoolHistorySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackedWools_Wools_WoolId",
                table: "TrackedWools");

            migrationBuilder.AlterColumn<int>(
                name: "WoolId",
                table: "TrackedWools",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "PatternType",
                table: "TrackedWools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "TrackedWools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoolBrand",
                table: "TrackedWools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoolColor",
                table: "TrackedWools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WoolLength",
                table: "TrackedWools",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoolMaterial",
                table: "TrackedWools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoolName",
                table: "TrackedWools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WoolNeedleMaxSize",
                table: "TrackedWools",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WoolNeedleMinSize",
                table: "TrackedWools",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WoolWeight",
                table: "TrackedWools",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedWools_Wools_WoolId",
                table: "TrackedWools",
                column: "WoolId",
                principalTable: "Wools",
                principalColumn: "WoolId",
                onDelete: ReferentialAction.SetNull);

            // Fige l'historique existant : caractéristiques de la laine et projet au moment de la migration.
            migrationBuilder.Sql(
                """
                UPDATE "TrackedWools"
                SET "WoolName" = (SELECT "Name" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolBrand" = (SELECT "Brand" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolMaterial" = (SELECT "Material" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolColor" = (SELECT "Color" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolWeight" = (SELECT "Weight" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolLength" = (SELECT "Length" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolNeedleMinSize" = (SELECT "NeedleMinSize" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId"),
                    "WoolNeedleMaxSize" = (SELECT "NeedleMaxSize" FROM "Wools" WHERE "Wools"."WoolId" = "TrackedWools"."WoolId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "TrackedWools"
                SET "ProjectName" = (SELECT "Name" FROM "Projects" WHERE "Projects"."ProjectId" = "TrackedWools"."ProjectId"),
                    "PatternType" = (
                        SELECT "Patterns"."Type"
                        FROM "Projects"
                        JOIN "Patterns" ON "Patterns"."PatternId" = "Projects"."PatternId"
                        WHERE "Projects"."ProjectId" = "TrackedWools"."ProjectId")
                WHERE "ProjectId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WoolId redevient obligatoire : les mouvements de laines supprimées ne peuvent pas être conservés.
            migrationBuilder.Sql(
                """
                DELETE FROM "TrackedWools"
                WHERE "WoolId" IS NULL OR "WoolId" NOT IN (SELECT "WoolId" FROM "Wools");
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_TrackedWools_Wools_WoolId",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "PatternType",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolBrand",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolColor",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolLength",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolMaterial",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolName",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolNeedleMaxSize",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolNeedleMinSize",
                table: "TrackedWools");

            migrationBuilder.DropColumn(
                name: "WoolWeight",
                table: "TrackedWools");

            migrationBuilder.AlterColumn<int>(
                name: "WoolId",
                table: "TrackedWools",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackedWools_Wools_WoolId",
                table: "TrackedWools",
                column: "WoolId",
                principalTable: "Wools",
                principalColumn: "WoolId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
