using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriasEmpireERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureWithholdingRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingRate",
                table: "CompanySettings",
                type: "decimal(6,4)",
                precision: 6,
                scale: 4,
                nullable: false,
                defaultValue: 0.025m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithholdingRate",
                table: "CompanySettings");
        }
    }
}
