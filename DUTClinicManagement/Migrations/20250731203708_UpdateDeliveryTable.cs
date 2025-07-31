using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeliveryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeliveryGuy_LicenseExpiryDate",
                table: "AspNetUsers",
                newName: "DeliveryPersonnel_LicenseExpiryDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeliveryPersonnel_LicenseExpiryDate",
                table: "AspNetUsers",
                newName: "DeliveryGuy_LicenseExpiryDate");
        }
    }
}
