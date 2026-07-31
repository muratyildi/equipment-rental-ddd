using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Eventing;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Queries;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.FleetAvailability.ReadModel;

public static class AvailabilityCalendarReadModel
{
    public static IServiceCollection AddAvailabilityCalendarReadModel(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AvailabilityCalendarDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    AvailabilityCalendarDbContext.Schema)));

        services.AddScoped<GetAvailabilityCalendarHandler>();
        services.AddSingleton<
            IIntegrationEventConsumer,
            AvailabilityCalendarProjectionConsumer>();

        return services;
    }
}
