using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarminePeople.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCheckedInToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCheckedIn",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCheckedIn",
                table: "AttendanceRecords");
        }
    }
}
