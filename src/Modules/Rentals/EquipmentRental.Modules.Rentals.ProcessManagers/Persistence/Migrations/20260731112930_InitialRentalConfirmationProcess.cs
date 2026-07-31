using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentRental.Modules.Rentals.ProcessManagers.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRentalConfirmationProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rentals_process_manager");

            migrationBuilder.CreateTable(
                name: "rental_confirmation_processes",
                schema: "rentals_process_manager",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rental_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deadline_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    claimed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_confirmation_processes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rental_confirmation_steps",
                schema: "rentals_process_manager",
                columns: table => new
                {
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rental_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    equipment_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfilment_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date_exclusive = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    commitment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_confirmation_steps", x => new { x.process_id, x.rental_line_id });
                    table.ForeignKey(
                        name: "FK_rental_confirmation_steps_rental_confirmation_processes_pro~",
                        column: x => x.process_id,
                        principalSchema: "rentals_process_manager",
                        principalTable: "rental_confirmation_processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_confirmation_process_rental_order",
                schema: "rentals_process_manager",
                table: "rental_confirmation_processes",
                column: "rental_order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rental_confirmation_steps",
                schema: "rentals_process_manager");

            migrationBuilder.DropTable(
                name: "rental_confirmation_processes",
                schema: "rentals_process_manager");
        }
    }
}
