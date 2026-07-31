using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Rentals.Application.Availability;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure.Availability;
using EquipmentRental.Modules.Rentals.Infrastructure.Eventing;
using EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.Rentals.Infrastructure;

public static class RentalsInfrastructure
{
    public static IServiceCollection AddRentalsInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<RentalsDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RentalsDbContext.Schema)));

        services.AddScoped<IRentalOrderRepository, PostgresRentalOrderRepository>();
        services.AddScoped<IEquipmentAvailabilityGateway, FleetAvailabilityGateway>();
        services.AddSingleton<IOutboxProcessor, RentalsOutboxProcessor>();

        return services;
    }
}
