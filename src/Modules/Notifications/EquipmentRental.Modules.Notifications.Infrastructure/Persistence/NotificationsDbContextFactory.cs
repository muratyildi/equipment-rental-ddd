using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentRental.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContextFactory
    : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EQUIPMENT_RENTAL_DATABASE")
            ?? "Host=localhost;Port=5432;Database=equipment_rental;Username=equipment_rental;Password=equipment_rental";

        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    NotificationsDbContext.Schema))
            .Options;

        return new NotificationsDbContext(options);
    }
}
