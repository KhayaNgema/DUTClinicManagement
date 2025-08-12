using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUTClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryRequests",
                columns: table => new
                {
                    DeliveryRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MedicationPescriptionId = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRequests", x => x.DeliveryRequestId);
                    table.ForeignKey(
                        name: "FK_DeliveryRequests_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DeliveryRequests_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryRequests_MedicationPescription_MedicationPescriptionId",
                        column: x => x.MedicationPescriptionId,
                        principalTable: "MedicationPescription",
                        principalColumn: "MedicationPescriptionId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRequests_MedicationPescriptionId",
                table: "DeliveryRequests",
                column: "MedicationPescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRequests_PatientId",
                table: "DeliveryRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRequests_UpdatedById",
                table: "DeliveryRequests",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryRequests");
        }
    }
}
