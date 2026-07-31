using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

public sealed class FleetAvailabilityDbContextFactory
    : IDesignTimeDbContextFactory<FleetAvailabilityDbContext>
{
    public FleetAvailabilityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EQUIPMENT_RENTAL_DATABASE")
            ?? "Host=localhost;Port=5432;Database=equipment_rental;Username=equipment_rental;Password=equipment_rental";

        var options = new DbContextOptionsBuilder<FleetAvailabilityDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    FleetAvailabilityDbContext.Schema))
            .Options;

        return new FleetAvailabilityDbContext(options);
    }
}
