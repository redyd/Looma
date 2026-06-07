// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Looma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatternDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BeginDate",
                table: "Patterns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Patterns",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeginDate",
                table: "Patterns");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Patterns");
        }
    }
}
