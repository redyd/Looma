using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrackedWool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedWools",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    WoolId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedWools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedWools_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrackedWools_Wools_WoolId",
                        column: x => x.WoolId,
                        principalTable: "Wools",
                        principalColumn: "WoolId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedWools_ProjectId",
                table: "TrackedWools",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedWools_WoolId",
                table: "TrackedWools",
                column: "WoolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedWools");
        }
    }
}
