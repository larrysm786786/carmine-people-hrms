using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarminePeople.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeWifiSsidToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfficeWifiSsid",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfficeWifiSsid",
                table: "Locations");
        }
    }
}
