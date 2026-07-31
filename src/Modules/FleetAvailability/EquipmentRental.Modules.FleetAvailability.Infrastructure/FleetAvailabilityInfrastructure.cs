using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Eventing;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure;

public static class FleetAvailabilityInfrastructure
{
    public static IServiceCollection AddFleetAvailabilityInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<FleetAvailabilityDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    FleetAvailabilityDbContext.Schema)));

        services.AddScoped<
            IAvailabilityScheduleRepository,
            PostgresAvailabilityScheduleRepository>();
        services.AddSingleton<IOutboxProcessor, FleetAvailabilityOutboxProcessor>();

        return services;
    }
}
