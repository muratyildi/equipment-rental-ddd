using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFleetAvailabilityPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fleet_availability");

            migrationBuilder.CreateTable(
                name: "availability_schedules",
                schema: "fleet_availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_capacity = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "availability_commitments",
                schema: "fleet_availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_demand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commitment_start = table.Column<DateOnly>(type: "date", nullable: false),
                    commitment_end_exclusive = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    availability_schedule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_commitments", x => x.id);
                    table.ForeignKey(
                        name: "FK_availability_commitments_availability_schedules_availabilit~",
                        column: x => x.availability_schedule_id,
                        principalSchema: "fleet_availability",
                        principalTable: "availability_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_availability_commitments_availability_schedule_id",
                schema: "fleet_availability",
                table: "availability_commitments",
                column: "availability_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ux_availability_commitments_external_demand",
                schema: "fleet_availability",
                table: "availability_commitments",
                column: "external_demand_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_availability_schedules_category_location",
                schema: "fleet_availability",
                table: "availability_schedules",
                columns: new[] { "equipment_category_id", "location_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_commitments",
                schema: "fleet_availability");

            migrationBuilder.DropTable(
                name: "availability_schedules",
                schema: "fleet_availability");
        }
    }
}
