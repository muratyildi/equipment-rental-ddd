using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Contracts.IntegrationEvents;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Eventing;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;
using EquipmentRental.Modules.FleetAvailability.ReadModel;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Eventing;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Queries;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using FleetCategoryId =
    EquipmentRental.Modules.FleetAvailability.Domain.Schedules.EquipmentCategoryId;

namespace EquipmentRental.Persistence.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class AvailabilityCalendarProjectionTests(PostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset OccurredAt =
        new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CapacityAndCommitmentEvents_BuildDailyCalendar()
    {
        var identity = NewScheduleIdentity();
        await using var services = CreateReadModelServices();
        var consumer = GetConsumer(services);

        await consumer.ConsumeAsync(
            CapacityEnvelope(identity, totalCapacity: 5),
            CancellationToken.None);
        await consumer.ConsumeAsync(
            CommitmentEnvelope(
                identity.ScheduleId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 13),
                quantity: 2),
            CancellationToken.None);

        var calendar = await QueryCalendarAsync(
            identity,
            new DateOnly(2026, 8, 9),
            new DateOnly(2026, 8, 14));

        Assert.NotNull(calendar);
        Assert.Collection(
            calendar.Days,
            day => AssertDay(day, new DateOnly(2026, 8, 9), 5, 0),
            day => AssertDay(day, new DateOnly(2026, 8, 10), 5, 2),
            day => AssertDay(day, new DateOnly(2026, 8, 11), 5, 2),
            day => AssertDay(day, new DateOnly(2026, 8, 12), 5, 2),
            day => AssertDay(day, new DateOnly(2026, 8, 13), 5, 0));
    }

    [Fact]
    public async Task DuplicateCommitmentEvent_DoesNotDoubleCount()
    {
        var identity = NewScheduleIdentity();
        var commitment = CommitmentEnvelope(
            identity.ScheduleId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12),
            quantity: 2);
        await using var services = CreateReadModelServices();
        var consumer = GetConsumer(services);

        await consumer.ConsumeAsync(
            CapacityEnvelope(identity, totalCapacity: 5),
            CancellationToken.None);
        await consumer.ConsumeAsync(commitment, CancellationToken.None);
        await consumer.ConsumeAsync(commitment, CancellationToken.None);

        var calendar = await QueryCalendarAsync(
            identity,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12));

        Assert.NotNull(calendar);
        Assert.All(
            calendar.Days,
            day => AssertDay(day, day.Date, 5, 2));

        await using var context = fixture.CreateAvailabilityCalendarContext();
        Assert.Equal(1, await context.InboxMessages.CountAsync(
            inbox => inbox.MessageId == commitment.Id));
    }

    [Fact]
    public async Task CommitmentBeforeCapacity_IsEventuallyQueryable()
    {
        var identity = NewScheduleIdentity();
        await using var services = CreateReadModelServices();
        var consumer = GetConsumer(services);

        await consumer.ConsumeAsync(
            CommitmentEnvelope(
                identity.ScheduleId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 11),
                quantity: 2),
            CancellationToken.None);

        Assert.Null(await QueryCalendarAsync(
            identity,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 11)));

        await consumer.ConsumeAsync(
            CapacityEnvelope(identity, totalCapacity: 5),
            CancellationToken.None);

        var calendar = await QueryCalendarAsync(
            identity,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 11));

        Assert.NotNull(calendar);
        AssertDay(Assert.Single(calendar.Days), new DateOnly(2026, 8, 10), 5, 2);
    }

    [Fact]
    public async Task ReleasedCommitment_RestoresProjectedCapacityIdempotently()
    {
        var identity = NewScheduleIdentity();
        var commitmentId = Guid.NewGuid();
        var release = ReleaseEnvelope(
            identity.ScheduleId,
            commitmentId,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12),
            quantity: 2);
        await using var services = CreateReadModelServices();
        var consumer = GetConsumer(services);

        await consumer.ConsumeAsync(
            CapacityEnvelope(identity, totalCapacity: 5),
            CancellationToken.None);
        await consumer.ConsumeAsync(
            CommitmentEnvelope(
                identity.ScheduleId,
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 12),
                quantity: 2,
                commitmentId: commitmentId),
            CancellationToken.None);
        await consumer.ConsumeAsync(release, CancellationToken.None);
        await consumer.ConsumeAsync(release, CancellationToken.None);

        var calendar = await QueryCalendarAsync(
            identity,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12));

        Assert.NotNull(calendar);
        Assert.All(
            calendar.Days,
            day => AssertDay(day, day.Date, 5, 0));
    }

    [Fact]
    public async Task FleetOutboxDelivery_ProjectsCalendarAndMarksMessagesProcessed()
    {
        await MarkExistingFleetMessagesProcessedAsync();

        var identity = NewScheduleIdentity();
        var schedule = AvailabilitySchedule.Define(
            AvailabilityScheduleId.From(identity.ScheduleId),
            FleetCategoryId.From(identity.EquipmentCategoryId),
            LocationId.From(identity.LocationId),
            5,
            OccurredAt);
        schedule.Commit(
            ExternalDemandId.From(Guid.NewGuid()),
            AvailabilityPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 12)),
            2,
            OccurredAt.AddMinutes(1));
        var eventIds = schedule.DomainEvents
            .Select(domainEvent => domainEvent.EventId)
            .ToArray();

        await using (var context = fixture.CreateFleetContext())
        {
            context.AvailabilitySchedules.Add(schedule);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var readServices = CreateReadModelServices();
        var publisher = new ConsumerPublisher(GetConsumer(readServices));
        var timeProvider =
            new ManualTimeProvider(OccurredAt.AddDays(1));
        var fleetServices = new ServiceCollection();
        fleetServices.AddDbContext<FleetAvailabilityDbContext>(
            options => options.UseNpgsql(
                fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    FleetAvailabilityDbContext.Schema)));
        await using var fleetServiceProvider =
            fleetServices.BuildServiceProvider();
        var processor = new FleetAvailabilityOutboxProcessor(
            fleetServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            publisher,
            timeProvider);

        var processed = await processor.ProcessBatchAsync(
            10,
            CancellationToken.None);

        Assert.Equal(2, processed);
        Assert.Equal(2, publisher.Messages.Count);

        var calendar = await QueryCalendarAsync(
            identity,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12));
        Assert.NotNull(calendar);
        Assert.All(
            calendar.Days,
            day => AssertDay(day, day.Date, 5, 2));

        await using var verificationContext = fixture.CreateFleetContext();
        var storedMessages = await verificationContext.OutboxMessages
            .Where(message => eventIds.Contains(message.Id))
            .ToArrayAsync();
        Assert.Equal(2, storedMessages.Length);
        Assert.All(
            storedMessages,
            message => Assert.Equal(
                timeProvider.GetUtcNow(),
                message.ProcessedAtUtc));
    }

    private ServiceProvider CreateReadModelServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(
            new ManualTimeProvider(OccurredAt.AddHours(1)));
        services.AddAvailabilityCalendarReadModel(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    private static AvailabilityCalendarProjectionConsumer GetConsumer(
        ServiceProvider services) =>
        Assert.IsType<AvailabilityCalendarProjectionConsumer>(
            Assert.Single(services.GetServices<IIntegrationEventConsumer>()));

    private async Task<AvailabilityCalendarSnapshot?> QueryCalendarAsync(
        ScheduleIdentity identity,
        DateOnly startDate,
        DateOnly endDateExclusive)
    {
        await using var context = fixture.CreateAvailabilityCalendarContext();
        var handler = new GetAvailabilityCalendarHandler(context);
        return await handler.Handle(
            new GetAvailabilityCalendar(
                identity.EquipmentCategoryId,
                identity.LocationId,
                startDate,
                endDateExclusive),
            CancellationToken.None);
    }

    private static IntegrationEventEnvelope CapacityEnvelope(
        ScheduleIdentity identity,
        int totalCapacity)
    {
        var eventId = Guid.NewGuid();
        var integrationEvent = new AvailabilityCapacityDefinedV1(
            eventId,
            identity.ScheduleId,
            identity.EquipmentCategoryId,
            identity.LocationId,
            totalCapacity,
            OccurredAt);

        return Envelope(
            eventId,
            AvailabilityCapacityDefinedV1.EventType,
            integrationEvent);
    }

    private static IntegrationEventEnvelope CommitmentEnvelope(
        Guid scheduleId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        int quantity,
        Guid? commitmentId = null)
    {
        var eventId = Guid.NewGuid();
        var integrationEvent = new EquipmentAvailabilityCommittedV1(
            eventId,
            scheduleId,
            commitmentId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            startDate,
            endDateExclusive,
            quantity,
            OccurredAt.AddMinutes(1));

        return Envelope(
            eventId,
            EquipmentAvailabilityCommittedV1.EventType,
            integrationEvent);
    }

    private static IntegrationEventEnvelope ReleaseEnvelope(
        Guid scheduleId,
        Guid commitmentId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        int quantity)
    {
        var eventId = Guid.NewGuid();
        var integrationEvent = new EquipmentAvailabilityReleasedV1(
            eventId,
            scheduleId,
            commitmentId,
            Guid.NewGuid(),
            startDate,
            endDateExclusive,
            quantity,
            OccurredAt.AddMinutes(2));

        return Envelope(
            eventId,
            EquipmentAvailabilityReleasedV1.EventType,
            integrationEvent);
    }

    private static IntegrationEventEnvelope Envelope<T>(
        Guid eventId,
        string eventType,
        T integrationEvent) =>
        new(
            eventId,
            eventType,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            OccurredAt);

    private static ScheduleIdentity NewScheduleIdentity() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static void AssertDay(
        AvailabilityCalendarDay day,
        DateOnly expectedDate,
        int total,
        int committed)
    {
        Assert.Equal(expectedDate, day.Date);
        Assert.Equal(total, day.TotalCapacity);
        Assert.Equal(committed, day.CommittedQuantity);
        Assert.Equal(total - committed, day.AvailableQuantity);
    }

    private async Task MarkExistingFleetMessagesProcessedAsync()
    {
        await using var context = fixture.CreateFleetContext();
        var messages = await context.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.MarkProcessed(OccurredAt);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private sealed record ScheduleIdentity(
        Guid ScheduleId,
        Guid EquipmentCategoryId,
        Guid LocationId);

    private sealed class ConsumerPublisher(IIntegrationEventConsumer consumer)
        : IIntegrationEventPublisher
    {
        private readonly List<IntegrationEventEnvelope> _messages = [];

        public List<IntegrationEventEnvelope> Messages => _messages;

        public async Task PublishAsync(
            IntegrationEventEnvelope message,
            CancellationToken cancellationToken)
        {
            _messages.Add(message);

            if (consumer.CanHandle(message.Type))
            {
                await consumer.ConsumeAsync(message, cancellationToken);
            }
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
