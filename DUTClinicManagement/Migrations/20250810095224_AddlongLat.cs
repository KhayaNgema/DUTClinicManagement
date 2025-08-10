using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddlongLat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PatientLatitude",
                table: "EmergencyRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PatientLongitude",
                table: "EmergencyRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientLatitude",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "PatientLongitude",
                table: "EmergencyRequests");
        }
    }
}
