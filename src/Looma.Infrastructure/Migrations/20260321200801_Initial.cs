using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                });

            migrationBuilder.CreateTable(
                name: "Patterns",
                columns: table => new
                {
                    PatternId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PatternType = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patterns", x => x.PatternId);
                });

            migrationBuilder.CreateTable(
                name: "Wools",
                columns: table => new
                {
                    WoolId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    LengthToWeightRatio = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wools", x => x.WoolId);
                });

            migrationBuilder.CreateTable(
                name: "DocumentPattern",
                columns: table => new
                {
                    DocumentsDocumentId = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BeginDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PatronId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_Patterns_PatronId",
                        column: x => x.PatronId,
                        principalTable: "Patterns",
                        principalColumn: "PatternId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    StockId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeightQuantity = table.Column<double>(type: "REAL", nullable: false),
                    WoolId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "WoolsForProjects",
                columns: table => new
                {
                    WoolId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoolsForProjects", x => new { x.WoolId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_WoolsForProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WoolsForProjects_Wools_WoolId",
                        column: x => x.WoolId,
                        principalTable: "Wools",
                        principalColumn: "WoolId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPattern_PatternsPatternId",
                table: "DocumentPattern",
                column: "PatternsPatternId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PatronId",
                table: "Projects",
                column: "PatronId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_WoolId",
                table: "Stocks",
                column: "WoolId");

            migrationBuilder.CreateIndex(
                name: "IX_WoolsForProjects_ProjectId",
                table: "WoolsForProjects",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentPattern");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "WoolsForProjects");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Wools");

            migrationBuilder.DropTable(
                name: "Patterns");
        }
    }
}
