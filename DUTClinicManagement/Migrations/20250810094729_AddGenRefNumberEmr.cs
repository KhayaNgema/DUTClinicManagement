using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddGenRefNumberEmr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "EmergencyRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "EmergencyRequests");
        }
    }
}
