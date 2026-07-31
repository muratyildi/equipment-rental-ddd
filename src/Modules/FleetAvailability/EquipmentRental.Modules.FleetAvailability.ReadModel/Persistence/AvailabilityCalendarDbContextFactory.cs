using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

public sealed class AvailabilityCalendarDbContextFactory
    : IDesignTimeDbContextFactory<AvailabilityCalendarDbContext>
{
    public AvailabilityCalendarDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EQUIPMENT_RENTAL_DATABASE")
            ?? "Host=localhost;Port=5432;Database=equipment_rental;Username=equipment_rental;Password=equipment_rental";

        var options = new DbContextOptionsBuilder<AvailabilityCalendarDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    AvailabilityCalendarDbContext.Schema))
            .Options;

        return new AvailabilityCalendarDbContext(options);
    }
}
