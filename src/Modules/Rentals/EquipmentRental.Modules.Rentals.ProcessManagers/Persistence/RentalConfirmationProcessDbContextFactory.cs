using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

public sealed class RentalConfirmationProcessDbContextFactory
    : IDesignTimeDbContextFactory<RentalConfirmationProcessDbContext>
{
    public RentalConfirmationProcessDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Port=5432;Database=equipment_rental;Username=postgres;Password=postgres";

        var options =
            new DbContextOptionsBuilder<RentalConfirmationProcessDbContext>()
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        RentalConfirmationProcessDbContext.Schema))
                .Options;

        return new RentalConfirmationProcessDbContext(options);
    }
}
