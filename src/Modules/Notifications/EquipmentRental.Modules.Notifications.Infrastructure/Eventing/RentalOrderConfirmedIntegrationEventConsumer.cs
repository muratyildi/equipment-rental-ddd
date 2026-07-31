using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Notifications.Application.RentalConfirmations;
using EquipmentRental.Modules.Notifications.Infrastructure.Persistence;
using EquipmentRental.Modules.Rentals.Contracts.IntegrationEvents;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace EquipmentRental.Modules.Notifications.Infrastructure.Eventing;

public sealed class RentalOrderConfirmedIntegrationEventConsumer(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider)
    : IIntegrationEventConsumer
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public string ConsumerName =>
        "notifications.rental-order-confirmed.v1";

    public bool CanHandle(string integrationEventType) =>
        string.Equals(
            integrationEventType,
            RentalOrderConfirmedV1.EventType,
            StringComparison.Ordinal);

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

        var integrationEvent =
            JsonSerializer.Deserialize<RentalOrderConfirmedV1>(
                message.Payload,
                SerializerOptions)
            ?? throw new JsonException(
                "Rental order confirmed payload cannot be null.");

        if (integrationEvent.EventId != message.Id)
        {
            throw new InvalidDataException(
                "Envelope id and integration event id must match.");
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<
            RequestRentalConfirmationNotificationHandler>();

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

        var now = timeProvider.GetUtcNow();
        var inboxMessage = InboxMessage.Receive(
            ConsumerName,
            message.Id,
            message.Type,
            now);
        dbContext.InboxMessages.Add(inboxMessage);

        await handler.Handle(
            new RequestRentalConfirmationNotification(
                integrationEvent.EventId,
                integrationEvent.RentalOrderId,
                integrationEvent.CustomerId,
                integrationEvent.StartDate,
                integrationEvent.EndDateExclusive,
                integrationEvent.EstimatedTotal,
                integrationEvent.Currency),
            cancellationToken);

        inboxMessage.MarkProcessed(now);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            if (await dbContext.InboxMessages.AnyAsync(
                    inbox =>
                        inbox.Consumer == ConsumerName
                        && inbox.MessageId == message.Id,
                    cancellationToken))
            {
                return;
            }

            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };
}
