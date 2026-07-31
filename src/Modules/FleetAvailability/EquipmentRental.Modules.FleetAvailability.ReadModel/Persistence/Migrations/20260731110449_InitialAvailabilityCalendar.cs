using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAvailabilityCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fleet_availability_read");

            migrationBuilder.CreateTable(
                name: "availability_days",
                schema: "fleet_availability_read",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    committed_quantity = table.Column<int>(type: "integer", nullable: false),
                    last_event_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_days", x => new { x.schedule_id, x.date });
                });

            migrationBuilder.CreateTable(
                name: "availability_schedules",
                schema: "fleet_availability_read",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_capacity = table.Column<int>(type: "integer", nullable: false),
                    last_event_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_schedules", x => x.schedule_id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "fleet_availability_read",
                columns: table => new
                {
                    consumer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.consumer, x.message_id });
                });

            migrationBuilder.CreateIndex(
                name: "ux_availability_schedules_category_location",
                schema: "fleet_availability_read",
                table: "availability_schedules",
                columns: new[] { "equipment_category_id", "location_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_days",
                schema: "fleet_availability_read");

            migrationBuilder.DropTable(
                name: "availability_schedules",
                schema: "fleet_availability_read");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "fleet_availability_read");
        }
    }
}
