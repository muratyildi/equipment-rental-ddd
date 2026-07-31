using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;
using EquipmentRental.Modules.Notifications.Infrastructure.Persistence;
using EquipmentRental.Modules.Rentals.Infrastructure.Persistence;
using EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace EquipmentRental.Persistence.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var rentalsContext = CreateRentalsContext();
        await rentalsContext.Database.MigrateAsync();

        await using var fleetContext = CreateFleetContext();
        await fleetContext.Database.MigrateAsync();

        await using var notificationsContext = CreateNotificationsContext();
        await notificationsContext.Database.MigrateAsync();

        await using var calendarContext = CreateAvailabilityCalendarContext();
        await calendarContext.Database.MigrateAsync();

        await using var processContext = CreateRentalConfirmationProcessContext();
        await processContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public RentalsDbContext CreateRentalsContext()
    {
        var options = new DbContextOptionsBuilder<RentalsDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RentalsDbContext.Schema))
            .Options;

        return new RentalsDbContext(options);
    }

    public FleetAvailabilityDbContext CreateFleetContext()
    {
        var options = new DbContextOptionsBuilder<FleetAvailabilityDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    FleetAvailabilityDbContext.Schema))
            .Options;

        return new FleetAvailabilityDbContext(options);
    }

    public NotificationsDbContext CreateNotificationsContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    NotificationsDbContext.Schema))
            .Options;

        return new NotificationsDbContext(options);
    }

    public AvailabilityCalendarDbContext CreateAvailabilityCalendarContext()
    {
        var options =
            new DbContextOptionsBuilder<AvailabilityCalendarDbContext>()
                .UseNpgsql(
                    ConnectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        AvailabilityCalendarDbContext.Schema))
                .Options;

        return new AvailabilityCalendarDbContext(options);
    }

    public RentalConfirmationProcessDbContext
        CreateRentalConfirmationProcessContext()
    {
        var options =
            new DbContextOptionsBuilder<RentalConfirmationProcessDbContext>()
                .UseNpgsql(
                    ConnectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        RentalConfirmationProcessDbContext.Schema))
                .Options;

        return new RentalConfirmationProcessDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlTestGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
