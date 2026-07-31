using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Notifications.Infrastructure;
using EquipmentRental.Modules.Notifications.Infrastructure.Eventing;
using EquipmentRental.Modules.Rentals.Contracts.IntegrationEvents;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure.Eventing;
using EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentRental.Persistence.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class IdempotentConsumerTests(PostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset OccurredAt =
        new(2026, 7, 31, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SameMessageDeliveredTwice_CreatesOneBusinessEffect()
    {
        var envelope = CreateEnvelope(Guid.NewGuid());
        await using var services = CreateNotificationsServices();
        var consumer = GetConsumer(services);

        await consumer.ConsumeAsync(envelope, CancellationToken.None);
        await consumer.ConsumeAsync(envelope, CancellationToken.None);

        await using var context = fixture.CreateNotificationsContext();
        Assert.Equal(1, await context.InboxMessages.CountAsync(
            inbox => inbox.MessageId == envelope.Id));
        Assert.Equal(1, await context.NotificationWorkItems.CountAsync(
            workItem => workItem.SourceEventId == envelope.Id));
    }

    [Fact]
    public async Task ConcurrentDuplicateDeliveries_CreateOneBusinessEffect()
    {
        var envelope = CreateEnvelope(Guid.NewGuid());
        await using var services = CreateNotificationsServices();
        var consumer = GetConsumer(services);

        await Task.WhenAll(
            consumer.ConsumeAsync(envelope, CancellationToken.None),
            consumer.ConsumeAsync(envelope, CancellationToken.None));

        await using var context = fixture.CreateNotificationsContext();
        Assert.Equal(1, await context.InboxMessages.CountAsync(
            inbox => inbox.MessageId == envelope.Id));
        Assert.Equal(1, await context.NotificationWorkItems.CountAsync(
            workItem => workItem.SourceEventId == envelope.Id));
    }

    [Fact]
    public async Task BusinessFailure_RollsBackInboxAndBusinessEffect()
    {
        var envelope = CreateEnvelope(
            Guid.NewGuid(),
            estimatedTotal: -1);
        await using var services = CreateNotificationsServices();
        var consumer = GetConsumer(services);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => consumer.ConsumeAsync(envelope, CancellationToken.None));

        await using var context = fixture.CreateNotificationsContext();
        Assert.False(await context.InboxMessages.AnyAsync(
            inbox => inbox.MessageId == envelope.Id));
        Assert.False(await context.NotificationWorkItems.AnyAsync(
            workItem => workItem.SourceEventId == envelope.Id));
    }

    [Fact]
    public async Task RentalsOutboxDelivery_ReachesConsumerExactlyOncePerMessageId()
    {
        await MarkExistingRentalsMessagesProcessedAsync();

        var order = CreateConfirmedOrder();
        var confirmed = Assert.Single(
            order.DomainEvents.OfType<RentalOrderConfirmed>());

        await using (var context = fixture.CreateRentalsContext())
        {
            context.RentalOrders.Add(order);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var notificationServices = CreateNotificationsServices();
        var consumer = GetConsumer(notificationServices);
        var publisher = new ConsumerPublisher(consumer);
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow.AddDays(1));

        var rentalServices = new ServiceCollection();
        rentalServices.AddDbContext<RentalsDbContext>(
            options => options.UseNpgsql(
                fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RentalsDbContext.Schema)));
        await using var rentalServiceProvider =
            rentalServices.BuildServiceProvider();
        var processor = new RentalsOutboxProcessor(
            rentalServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            publisher,
            timeProvider);

        var processed = await processor.ProcessBatchAsync(
            10,
            CancellationToken.None);
        await publisher.PublishAsync(
            Assert.Single(publisher.Messages),
            CancellationToken.None);

        Assert.Equal(1, processed);

        await using var notificationsContext = fixture.CreateNotificationsContext();
        Assert.Equal(1, await notificationsContext.InboxMessages.CountAsync(
            inbox => inbox.MessageId == confirmed.EventId));
        Assert.Equal(1, await notificationsContext.NotificationWorkItems.CountAsync(
            workItem => workItem.SourceEventId == confirmed.EventId));
    }

    private ServiceProvider CreateNotificationsServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(
            new ManualTimeProvider(OccurredAt.AddHours(1)));
        services.AddNotificationsInfrastructure(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    private static RentalOrderConfirmedIntegrationEventConsumer GetConsumer(
        ServiceProvider services) =>
        Assert.IsType<RentalOrderConfirmedIntegrationEventConsumer>(
            Assert.Single(services.GetServices<IIntegrationEventConsumer>()));

    private static IntegrationEventEnvelope CreateEnvelope(
        Guid eventId,
        decimal estimatedTotal = 6000)
    {
        var integrationEvent = new RentalOrderConfirmedV1(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 13),
            estimatedTotal,
            "TRY",
            OccurredAt);

        return new IntegrationEventEnvelope(
            eventId,
            RentalOrderConfirmedV1.EventType,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            OccurredAt);
    }

    private static RentalOrder CreateConfirmedOrder()
    {
        var order = RentalOrder.Draft(
            RentalOrderId.New(),
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            RentalPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 13)),
            OccurredAt);
        var lineId = order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            2,
            Money.Of(1000, "TRY"),
            OccurredAt);
        order.Quote(OccurredAt.AddHours(2), OccurredAt.AddMinutes(1));
        order.RecordAvailabilityCommitment(
            lineId,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            order.Period,
            OccurredAt.AddMinutes(2));
        order.Confirm(OccurredAt.AddMinutes(3));
        return order;
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

    private sealed class ConsumerPublisher(IIntegrationEventConsumer consumer)
        : IIntegrationEventPublisher
    {
        private readonly List<IntegrationEventEnvelope> _messages = [];

        public IReadOnlyCollection<IntegrationEventEnvelope> Messages => _messages;

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
