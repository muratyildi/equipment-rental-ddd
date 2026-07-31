using EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.Rentals.ProcessManagers;

public static class RentalsProcessManagers
{
    public static IServiceCollection AddRentalsProcessManagers(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<RentalConfirmationProcessDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RentalConfirmationProcessDbContext.Schema)));
        services.AddScoped<StartRentalConfirmationHandler>();
        services.AddScoped<GetRentalConfirmationHandler>();
        services.AddSingleton<RentalConfirmationProcessProcessor>();

        return services;
    }
}
