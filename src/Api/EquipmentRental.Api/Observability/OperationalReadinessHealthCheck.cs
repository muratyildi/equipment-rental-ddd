using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;
using EquipmentRental.Modules.Notifications.Infrastructure.Persistence;
using EquipmentRental.Modules.Rentals.Infrastructure.Persistence;
using EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EquipmentRental.Api.Observability;

public sealed class OperationalReadinessHealthCheck(
    IServiceScopeFactory scopeFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var contexts = new DbContext[]
        {
            services.GetRequiredService<RentalsDbContext>(),
            services.GetRequiredService<FleetAvailabilityDbContext>(),
            services.GetRequiredService<NotificationsDbContext>(),
            services.GetRequiredService<AvailabilityCalendarDbContext>(),
            services.GetRequiredService<RentalConfirmationProcessDbContext>(),
        };

        foreach (var dbContext in contexts)
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy(
                    $"{dbContext.GetType().Name} cannot connect.");
            }
        }

        var rentals = (RentalsDbContext)contexts[0];
        var fleet = (FleetAvailabilityDbContext)contexts[1];
        var processes = (RentalConfirmationProcessDbContext)contexts[4];
        var deadLetters =
            await rentals.OutboxMessages.CountAsync(
                message => message.DeadLetteredAtUtc != null,
                cancellationToken)
            + await fleet.OutboxMessages.CountAsync(
                message => message.DeadLetteredAtUtc != null,
                cancellationToken);
        var interventionCount = await processes.Processes.CountAsync(
            process =>
                process.Status ==
                RentalConfirmationProcessStatus.RequiresIntervention,
            cancellationToken);

        if (deadLetters > 0 || interventionCount > 0)
        {
            return HealthCheckResult.Degraded(
                "Operational intervention is required.",
                data: new Dictionary<string, object>
                {
                    ["deadLetterMessages"] = deadLetters,
                    ["processesRequiringIntervention"] = interventionCount,
                });
        }

        return HealthCheckResult.Healthy();
    }
}
