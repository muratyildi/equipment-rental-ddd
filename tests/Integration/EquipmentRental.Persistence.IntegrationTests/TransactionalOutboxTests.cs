using System.Collections.Concurrent;
using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Contracts.IntegrationEvents;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;
using EquipmentRental.Modules.Rentals.Contracts.IntegrationEvents;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure.Eventing;
using EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using FleetCategoryId =
    EquipmentRental.Modules.FleetAvailability.Domain.Schedules.EquipmentCategoryId;
using RentalCategoryId =
    EquipmentRental.Modules.Rentals.Domain.RentalOrders.EquipmentCategoryId;
using RentalCommitmentId =
    EquipmentRental.Modules.Rentals.Domain.RentalOrders.AvailabilityCommitmentId;

namespace EquipmentRental.Persistence.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class TransactionalOutboxTests(PostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset OccurredAt =
        new(2026, 7, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConfirmedRental_IsMappedToVersionedIntegrationEvent()
    {
        var order = CreateConfirmedOrder(RentalOrderId.New());
        var confirmed = Assert.Single(
            order.DomainEvents.OfType<RentalOrderConfirmed>());

        await using (var context = fixture.CreateRentalsContext())
        {
            context.RentalOrders.Add(order);
            await context.SaveChangesAsync(CancellationToken.None);

            Assert.Empty(order.DomainEvents);
        }

        await using var verificationContext = fixture.CreateRentalsContext();
        var outboxMessage = await verificationContext.OutboxMessages
            .SingleAsync(message => message.Id == confirmed.EventId);
        var integrationEvent =
            JsonSerializer.Deserialize<RentalOrderConfirmedV1>(
                outboxMessage.Payload,
                SerializerOptions);

        Assert.Equal(RentalOrderConfirmedV1.EventType, outboxMessage.Type);
        Assert.NotNull(integrationEvent);
        Assert.Equal(order.Id.Value, integrationEvent.RentalOrderId);
        Assert.Equal(order.EstimatedTotal!.Amount, integrationEvent.EstimatedTotal);
        Assert.Equal(order.EstimatedTotal.Currency, integrationEvent.Currency);
        Assert.Null(outboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task FailedAggregateSave_DoesNotLeaveAnOrphanOutboxMessage()
    {
        var duplicatedId = RentalOrderId.New();
        var existing = RentalOrder.Draft(
            duplicatedId,
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            RentalPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 12)),
            OccurredAt);

        await using (var seedContext = fixture.CreateRentalsContext())
        {
            seedContext.RentalOrders.Add(existing);
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        var duplicate = CreateConfirmedOrder(duplicatedId);
        var confirmed = Assert.Single(
            duplicate.DomainEvents.OfType<RentalOrderConfirmed>());

        await using (var failingContext = fixture.CreateRentalsContext())
        {
            failingContext.RentalOrders.Add(duplicate);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => failingContext.SaveChangesAsync(CancellationToken.None));
        }

        Assert.Contains(
            duplicate.DomainEvents,
            domainEvent => domainEvent.EventId == confirmed.EventId);

        await using var verificationContext = fixture.CreateRentalsContext();
        Assert.False(await verificationContext.OutboxMessages.AnyAsync(
            message => message.Id == confirmed.EventId));
    }

    [Fact]
    public async Task FleetCommitment_IsWrittenToItsOwnSchemaOutbox()
    {
        var schedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.New(),
            FleetCategoryId.From(Guid.NewGuid()),
            LocationId.From(Guid.NewGuid()),
            3,
            OccurredAt);
        var decision = schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            AvailabilityPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 12)),
            2,
            OccurredAt.AddMinutes(1));
        var committed = Assert.Single(
            schedule.DomainEvents.OfType<EquipmentAvailabilityCommitted>());

        await using (var context = fixture.CreateFleetContext())
        {
            context.AvailabilitySchedules.Add(schedule);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var verificationContext = fixture.CreateFleetContext();
        var outboxMessage = await verificationContext.OutboxMessages
            .SingleAsync(message => message.Id == committed.EventId);
        var integrationEvent =
            JsonSerializer.Deserialize<EquipmentAvailabilityCommittedV1>(
                outboxMessage.Payload,
                SerializerOptions);

        Assert.True(decision.Accepted);
        Assert.Equal(
            EquipmentAvailabilityCommittedV1.EventType,
            outboxMessage.Type);
        Assert.NotNull(integrationEvent);
        Assert.Equal(decision.CommitmentId!.Value, integrationEvent.CommitmentId);
        Assert.Equal(2, integrationEvent.Quantity);
    }

    [Fact]
    public async Task SuccessfulPublication_MarksMessageAsProcessed()
    {
        await MarkExistingRentalsMessagesProcessedAsync();

        var order = CreateConfirmedOrder(RentalOrderId.New());
        var confirmed = Assert.Single(
            order.DomainEvents.OfType<RentalOrderConfirmed>());

        await using (var context = fixture.CreateRentalsContext())
        {
            context.RentalOrders.Add(order);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var publisher = new RecordingIntegrationEventPublisher();
        var timeProvider = new ManualTimeProvider(OccurredAt.AddDays(1));
        await using var services = CreateProcessorServices(publisher, timeProvider);
        var processor = new RentalsOutboxProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            publisher,
            timeProvider);

        var processed = await processor.ProcessBatchAsync(
            10,
            CancellationToken.None);

        Assert.Equal(1, processed);
        var published = Assert.Single(publisher.Messages);
        Assert.Equal(confirmed.EventId, published.Id);

        await using var verificationContext = fixture.CreateRentalsContext();
        var stored = await verificationContext.OutboxMessages
            .SingleAsync(message => message.Id == confirmed.EventId);
        Assert.Equal(timeProvider.GetUtcNow(), stored.ProcessedAtUtc);
        Assert.Null(stored.ClaimedBy);
    }

    [Fact]
    public async Task FailedPublication_ReleasesClaimAndSchedulesRetry()
    {
        await MarkExistingRentalsMessagesProcessedAsync();

        var order = CreateConfirmedOrder(RentalOrderId.New());
        var confirmed = Assert.Single(
            order.DomainEvents.OfType<RentalOrderConfirmed>());

        await using (var context = fixture.CreateRentalsContext())
        {
            context.RentalOrders.Add(order);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var publisher = new RecordingIntegrationEventPublisher(
            new InvalidOperationException("Broker unavailable."));
        var timeProvider = new ManualTimeProvider(OccurredAt.AddDays(1));
        await using var services = CreateProcessorServices(publisher, timeProvider);
        var processor = new RentalsOutboxProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            publisher,
            timeProvider);

        var processed = await processor.ProcessBatchAsync(
            10,
            CancellationToken.None);

        Assert.Equal(0, processed);

        await using var verificationContext = fixture.CreateRentalsContext();
        var stored = await verificationContext.OutboxMessages
            .SingleAsync(message => message.Id == confirmed.EventId);
        Assert.Null(stored.ProcessedAtUtc);
        Assert.Null(stored.ClaimedBy);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("Broker unavailable.", stored.LastError);
        Assert.True(stored.NextAttemptAtUtc > timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task RepeatedPublicationFailure_DeadLettersAfterFiveAttempts()
    {
        await MarkExistingRentalsMessagesProcessedAsync();

        var order = CreateConfirmedOrder(RentalOrderId.New());
        var confirmed = Assert.Single(
            order.DomainEvents.OfType<RentalOrderConfirmed>());
        await using (var context = fixture.CreateRentalsContext())
        {
            context.RentalOrders.Add(order);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var publisher = new RecordingIntegrationEventPublisher(
            new InvalidOperationException("Permanent broker rejection."));
        var timeProvider = new ManualTimeProvider(OccurredAt.AddDays(1));
        await using var services = CreateProcessorServices(
            publisher,
            timeProvider);
        var processor = new RentalsOutboxProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            publisher,
            timeProvider);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await processor.ProcessBatchAsync(1, CancellationToken.None);
            timeProvider.Advance(TimeSpan.FromMinutes(2));
        }

        await processor.ProcessBatchAsync(1, CancellationToken.None);

        await using var verificationContext = fixture.CreateRentalsContext();
        var stored = await verificationContext.OutboxMessages
            .SingleAsync(message => message.Id == confirmed.EventId);
        Assert.Equal(5, stored.Attempts);
        Assert.NotNull(stored.DeadLetteredAtUtc);
        Assert.Null(stored.ProcessedAtUtc);
    }

    private static RentalOrder CreateConfirmedOrder(RentalOrderId id)
    {
        var order = RentalOrder.Draft(
            id,
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            RentalPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 13)),
            OccurredAt);
        var lineId = order.AddLine(
            RentalCategoryId.From(Guid.NewGuid()),
            2,
            Money.Of(1000, "TRY"),
            OccurredAt);
        order.Quote(OccurredAt.AddHours(2), OccurredAt.AddMinutes(1));
        order.RecordAvailabilityCommitment(
            lineId,
            RentalCommitmentId.From(Guid.NewGuid()),
            order.Period,
            OccurredAt.AddMinutes(2));
        order.Confirm(OccurredAt.AddMinutes(3));

        return order;
    }

    private ServiceProvider CreateProcessorServices(
        IIntegrationEventPublisher publisher,
        TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(publisher);
        services.AddSingleton(timeProvider);
        services.AddDbContext<RentalsDbContext>(
            options => options.UseNpgsql(
                fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RentalsDbContext.Schema)));

        return services.BuildServiceProvider();
    }

    private async Task MarkExistingRentalsMessagesProcessedAsync()
    {
        await using var context = fixture.CreateRentalsContext();
        var messages = await context.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.MarkProcessed(OccurredAt);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private sealed class RecordingIntegrationEventPublisher(
        Exception? exception = null)
        : IIntegrationEventPublisher
    {
        private readonly ConcurrentQueue<IntegrationEventEnvelope> _messages = [];

        public IReadOnlyCollection<IntegrationEventEnvelope> Messages =>
            _messages.ToArray();

        public Task PublishAsync(
            IntegrationEventEnvelope message,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            _messages.Enqueue(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
