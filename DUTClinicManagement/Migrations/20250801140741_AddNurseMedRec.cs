using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddNurseMedRec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalHistorys_AspNetUsers_DoctorId",
                table: "MedicalHistorys");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorId",
                table: "MedicalHistorys",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "NurseId",
                table: "MedicalHistorys",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalHistorys_NurseId",
                table: "MedicalHistorys",
                column: "NurseId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalHistorys_AspNetUsers_DoctorId",
                table: "MedicalHistorys",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalHistorys_AspNetUsers_NurseId",
                table: "MedicalHistorys",
                column: "NurseId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalHistorys_AspNetUsers_DoctorId",
                table: "MedicalHistorys");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalHistorys_AspNetUsers_NurseId",
                table: "MedicalHistorys");

            migrationBuilder.DropIndex(
                name: "IX_MedicalHistorys_NurseId",
                table: "MedicalHistorys");

            migrationBuilder.DropColumn(
                name: "NurseId",
                table: "MedicalHistorys");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorId",
                table: "MedicalHistorys",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalHistorys_AspNetUsers_DoctorId",
                table: "MedicalHistorys",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
