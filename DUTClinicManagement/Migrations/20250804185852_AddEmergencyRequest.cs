using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvalable",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Paramedic_Education",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Paramedic_LicenseNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Paramedic_YearsOfExperience",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmergencyRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedParamedicIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParamedicId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EmergencyDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestStatus = table.Column<int>(type: "int", nullable: true),
                    RequestLocation = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_EmergencyRequests_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_EmergencyRequests_AspNetUsers_ParamedicId",
                        column: x => x.ParamedicId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmergencyRequests_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_ModifiedById",
                table: "EmergencyRequests",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_ParamedicId",
                table: "EmergencyRequests",
                column: "ParamedicId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRequests_PatientId",
                table: "EmergencyRequests",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyRequests");

            migrationBuilder.DropColumn(
                name: "IsAvalable",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Paramedic_Education",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Paramedic_LicenseNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Paramedic_YearsOfExperience",
                table: "AspNetUsers");
        }
    }
}
