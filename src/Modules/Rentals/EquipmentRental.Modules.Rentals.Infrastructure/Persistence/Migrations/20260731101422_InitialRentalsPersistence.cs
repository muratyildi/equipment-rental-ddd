using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRentalsPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rentals");

            migrationBuilder.CreateTable(
                name: "rental_orders",
                schema: "rentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfilment_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rental_start = table.Column<DateOnly>(type: "date", nullable: false),
                    rental_end_exclusive = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    estimated_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    estimated_total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    quote_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rental_lines",
                schema: "rentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    daily_rate_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    daily_rate_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    availability_commitment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rental_order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_rental_lines_rental_orders_rental_order_id",
                        column: x => x.rental_order_id,
                        principalSchema: "rentals",
                        principalTable: "rental_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rental_lines_rental_order_id",
                schema: "rentals",
                table: "rental_lines",
                column: "rental_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rental_lines",
                schema: "rentals");

            migrationBuilder.DropTable(
                name: "rental_orders",
                schema: "rentals");
        }
    }
}
