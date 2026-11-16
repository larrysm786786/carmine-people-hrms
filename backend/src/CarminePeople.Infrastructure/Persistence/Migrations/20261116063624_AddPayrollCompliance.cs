using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarminePeople.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EsiEmployeeAmount",
                table: "Payslips",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EsiEmployerAmount",
                table: "Payslips",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PfEmployeeAmount",
                table: "Payslips",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PfEmployerAmount",
                table: "Payslips",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TdsAmount",
                table: "Payslips",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollComplianceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPfEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PfEmployeeRatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PfEmployerRatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PfWageCeiling = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    IsEsiEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EsiEmployeeRatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EsiEmployerRatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EsiEligibilityThreshold = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    IsTdsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollComplianceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TdsSlabs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpToAnnualIncome = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    RatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TdsSlabs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollComplianceSettings");

            migrationBuilder.DropTable(
                name: "TdsSlabs");

            migrationBuilder.DropColumn(
                name: "EsiEmployeeAmount",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "EsiEmployerAmount",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "PfEmployeeAmount",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "PfEmployerAmount",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "TdsAmount",
                table: "Payslips");
        }
    }
}
