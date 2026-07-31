using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Contracts.IntegrationEvents;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Eventing;

public sealed class AvailabilityCalendarProjectionConsumer(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider)
    : IIntegrationEventConsumer
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public string ConsumerName =>
        "fleet-availability.availability-calendar.v1";

    public bool CanHandle(string integrationEventType) =>
        integrationEventType is
            AvailabilityCapacityDefinedV1.EventType
            or EquipmentAvailabilityCommittedV1.EventType
            or EquipmentAvailabilityReleasedV1.EventType;

    public async Task ConsumeAsync(
        IntegrationEventEnvelope message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!CanHandle(message.Type))
        {
            throw new InvalidOperationException(
                $"Consumer '{ConsumerName}' cannot handle '{message.Type}'.");
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await ProjectAsync(message, cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 2)
            {
            }
            catch (DbUpdateException exception) when (
                IsUniqueViolation(exception)
                && attempt < 2)
            {
            }
        }
    }

    private async Task ProjectAsync(
        IntegrationEventEnvelope message,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AvailabilityCalendarDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        if (await dbContext.InboxMessages.AnyAsync(
                inbox =>
                    inbox.Consumer == ConsumerName
                    && inbox.MessageId == message.Id,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        switch (message.Type)
        {
            case AvailabilityCapacityDefinedV1.EventType:
                await ProjectCapacityDefinedAsync(
                    dbContext,
                    Deserialize<AvailabilityCapacityDefinedV1>(message),
                    cancellationToken);
                break;

            case EquipmentAvailabilityCommittedV1.EventType:
                var committed =
                    Deserialize<EquipmentAvailabilityCommittedV1>(message);
                await ProjectQuantityDeltaAsync(
                    dbContext,
                    committed.ScheduleId,
                    committed.StartDate,
                    committed.EndDateExclusive,
                    committed.Quantity,
                    committed.OccurredAtUtc,
                    cancellationToken);
                break;

            case EquipmentAvailabilityReleasedV1.EventType:
                var released =
                    Deserialize<EquipmentAvailabilityReleasedV1>(message);
                await ProjectQuantityDeltaAsync(
                    dbContext,
                    released.ScheduleId,
                    released.StartDate,
                    released.EndDateExclusive,
                    -released.Quantity,
                    released.OccurredAtUtc,
                    cancellationToken);
                break;
        }

        dbContext.InboxMessages.Add(
            ProjectionInboxMessage.Processed(
                ConsumerName,
                message.Id,
                message.Type,
                timeProvider.GetUtcNow()));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ProjectCapacityDefinedAsync(
        AvailabilityCalendarDbContext dbContext,
        AvailabilityCapacityDefinedV1 integrationEvent,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Schedules.FindAsync(
            [integrationEvent.ScheduleId],
            cancellationToken);

        if (existing is null)
        {
            dbContext.Schedules.Add(
                AvailabilityScheduleProjection.Create(
                    integrationEvent.ScheduleId,
                    integrationEvent.EquipmentCategoryId,
                    integrationEvent.LocationId,
                    integrationEvent.TotalCapacity,
                    integrationEvent.OccurredAtUtc));
            return;
        }

        if (!existing.HasSameDefinition(
                integrationEvent.EquipmentCategoryId,
                integrationEvent.LocationId,
                integrationEvent.TotalCapacity))
        {
            throw new InvalidDataException(
                "A schedule projection cannot be redefined with different terms.");
        }
    }

    private static async Task ProjectQuantityDeltaAsync(
        AvailabilityCalendarDbContext dbContext,
        Guid scheduleId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        int quantityDelta,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        for (var date = startDate;
             date < endDateExclusive;
             date = date.AddDays(1))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO fleet_availability_read.availability_days
                    (schedule_id, date, committed_quantity, last_event_at_utc)
                VALUES
                    ({scheduleId}, {date}, {quantityDelta}, {occurredAtUtc})
                ON CONFLICT (schedule_id, date)
                DO UPDATE SET
                    committed_quantity = availability_days.committed_quantity + EXCLUDED.committed_quantity,
                    last_event_at_utc = GREATEST(availability_days.last_event_at_utc, EXCLUDED.last_event_at_utc);
                """,
                cancellationToken);
        }
    }

    private static TIntegrationEvent Deserialize<TIntegrationEvent>(
        IntegrationEventEnvelope message)
        where TIntegrationEvent : class
    {
        var integrationEvent = JsonSerializer.Deserialize<TIntegrationEvent>(
            message.Payload,
            SerializerOptions)
            ?? throw new JsonException(
                $"Integration event '{message.Type}' payload cannot be null.");

        var payloadEventId = integrationEvent switch
        {
            AvailabilityCapacityDefinedV1 capacityDefined =>
                capacityDefined.EventId,
            EquipmentAvailabilityCommittedV1 committed =>
                committed.EventId,
            EquipmentAvailabilityReleasedV1 released =>
                released.EventId,
            _ => Guid.Empty,
        };

        if (payloadEventId != message.Id)
        {
            throw new InvalidDataException(
                "Envelope id and integration event id must match.");
        }

        return integrationEvent;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };
}
