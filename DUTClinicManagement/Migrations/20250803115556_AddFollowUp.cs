using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_DoctorId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BodyParts",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ScannerImage",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "NurseId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_NurseId",
                table: "Bookings",
                column: "NurseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_DoctorId",
                table: "Bookings",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_NurseId",
                table: "Bookings",
                column: "NurseId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_DoctorId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_NurseId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_NurseId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NurseId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "BodyParts",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BookingAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannerImage",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_DoctorId",
                table: "Bookings",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
