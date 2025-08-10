using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddParamedicCord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ParamedicLatitude",
                table: "EmergencyRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ParamedicLongitude",
                table: "EmergencyRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParamedicLatitude",
                table: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "ParamedicLongitude",
                table: "EmergencyRequests");
        }
    }
}
