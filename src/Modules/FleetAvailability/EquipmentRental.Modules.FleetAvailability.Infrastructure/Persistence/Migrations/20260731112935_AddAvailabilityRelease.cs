using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "released_at_utc",
                schema: "fleet_availability",
                table: "availability_commitments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "released_at_utc",
                schema: "fleet_availability",
                table: "availability_commitments");
        }
    }
}
