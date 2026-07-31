using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dead_lettered_at_utc",
                schema: "rentals",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dead_lettered_at_utc",
                schema: "rentals",
                table: "outbox_messages");
        }
    }
}
