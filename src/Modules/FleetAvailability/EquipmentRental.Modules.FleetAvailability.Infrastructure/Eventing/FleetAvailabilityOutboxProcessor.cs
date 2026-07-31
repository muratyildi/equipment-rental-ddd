using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Eventing;

public sealed class FleetAvailabilityOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider)
    : IOutboxProcessor
{
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(2);
    private const int MaximumAttempts = 5;

    public string ModuleName => "FleetAvailability";

    public async Task<int> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        using var scope = scopeFactory.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<FleetAvailabilityDbContext>();
        var workerId = Guid.NewGuid();
        var processedCount = 0;

        for (var index = 0; index < batchSize; index++)
        {
            var message = await TryClaimNextAsync(
                dbContext,
                workerId,
                cancellationToken);

            if (message is null)
            {
                break;
            }

            try
            {
                await publisher.PublishAsync(
                    message.ToEnvelope(),
                    cancellationToken);

                message.MarkProcessed(timeProvider.GetUtcNow());
                await dbContext.SaveChangesAsync(cancellationToken);
                OperationalTelemetry.OutboxPublished.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "messaging.system",
                        ModuleName));
                processedCount++;
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException
                || !cancellationToken.IsCancellationRequested)
            {
                var failedAtUtc = timeProvider.GetUtcNow();
                var retryAtUtc = failedAtUtc.Add(
                    CalculateBackoff(message.Attempts + 1));
                message.MarkFailed(
                    exception.Message,
                    retryAtUtc,
                    failedAtUtc,
                    MaximumAttempts);
                await dbContext.SaveChangesAsync(cancellationToken);
                OperationalTelemetry.OutboxFailed.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "messaging.system",
                        ModuleName));

                if (message.DeadLetteredAtUtc is not null)
                {
                    OperationalTelemetry.OutboxDeadLettered.Add(
                        1,
                        new KeyValuePair<string, object?>(
                            "messaging.system",
                            ModuleName));
                }
            }
        }

        return processedCount;
    }

    private async Task<OutboxMessage?> TryClaimNextAsync(
        FleetAvailabilityDbContext dbContext,
        Guid workerId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var now = timeProvider.GetUtcNow();
            var message = await dbContext.OutboxMessages
                .Where(candidate =>
                    candidate.ProcessedAtUtc == null
                    && candidate.DeadLetteredAtUtc == null
                    && candidate.NextAttemptAtUtc <= now
                    && (candidate.ClaimedUntilUtc == null
                        || candidate.ClaimedUntilUtc < now))
                .OrderBy(candidate => candidate.OccurredAtUtc)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (message is null)
            {
                return null;
            }

            message.Claim(workerId, now.Add(ClaimDuration));

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return message;
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return null;
    }

    private static TimeSpan CalculateBackoff(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 60));
}
