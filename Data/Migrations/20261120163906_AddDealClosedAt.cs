using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriasEmpireERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDealClosedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "BusinessDeals",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "BusinessDeals");
        }
    }
}
