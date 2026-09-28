using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarminePeople.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceEmbedding",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FaceEnrolledAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FaceVerified",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceEmbedding",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "FaceEnrolledAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "FaceVerified",
                table: "AttendanceRecords");
        }
    }
}
