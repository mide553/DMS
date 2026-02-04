using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperlessREST.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessCount",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessDate",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessCount",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "LastAccessDate",
                table: "Documents");
        }
    }
}
