using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Notifications.Application.RentalConfirmations;
using EquipmentRental.Modules.Notifications.Infrastructure.Eventing;
using EquipmentRental.Modules.Notifications.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.Notifications.Infrastructure;

public static class NotificationsInfrastructure
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<NotificationsDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    NotificationsDbContext.Schema)));

        services.AddScoped<
            INotificationWorkItemWriter,
            PostgresNotificationWorkItemWriter>();
        services.AddScoped<RequestRentalConfirmationNotificationHandler>();
        services.AddSingleton<
            IIntegrationEventConsumer,
            RentalOrderConfirmedIntegrationEventConsumer>();

        return services;
    }
}
