using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriasEmpireERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentReceiptsAndPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "ExpenseRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ExpensePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseRequestId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_ExpenseRequests_ExpenseRequestId",
                        column: x => x.ExpenseRequestId,
                        principalTable: "ExpenseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodDays = table.Column<int>(type: "int", nullable: false),
                    DaysWorked = table.Column<int>(type: "int", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncapacityDays = table.Column<int>(type: "int", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_ExpenseRequestId",
                table: "ExpensePayments",
                column: "ExpenseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_EmployeeId_PeriodStart_PeriodDays",
                table: "PayrollPayments",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodDays" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpensePayments");

            migrationBuilder.DropTable(
                name: "PayrollPayments");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "ExpenseRequests");
        }
    }
}
