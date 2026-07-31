using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

public sealed class RentalsDbContextFactory
    : IDesignTimeDbContextFactory<RentalsDbContext>
{
    public RentalsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EQUIPMENT_RENTAL_DATABASE")
            ?? "Host=localhost;Port=5432;Database=equipment_rental;Username=equipment_rental;Password=equipment_rental";

        var options = new DbContextOptionsBuilder<RentalsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RentalsDbContext.Schema))
            .Options;

        return new RentalsDbContext(options);
    }
}
