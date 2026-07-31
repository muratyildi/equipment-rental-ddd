using System.Text.Json;

using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Rentals.Contracts.IntegrationEvents;
using EquipmentRental.Modules.Rentals.Domain.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Eventing;

internal static class RentalsIntegrationEventMapper
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static OutboxMessage? Map(
        IDomainEvent domainEvent,
        DateTimeOffset createdAtUtc) =>
        domainEvent switch
        {
            RentalOrderConfirmed confirmed => Map(confirmed, createdAtUtc),
            _ => null,
        };

    private static OutboxMessage Map(
        RentalOrderConfirmed domainEvent,
        DateTimeOffset createdAtUtc)
    {
        var integrationEvent = new RentalOrderConfirmedV1(
            domainEvent.EventId,
            domainEvent.RentalOrderId.Value,
            domainEvent.CustomerId.Value,
            domainEvent.Period.Start,
            domainEvent.Period.EndExclusive,
            domainEvent.EstimatedTotal.Amount,
            domainEvent.EstimatedTotal.Currency,
            domainEvent.OccurredAtUtc);

        return OutboxMessage.Create(
            domainEvent.EventId,
            RentalOrderConfirmedV1.EventType,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            domainEvent.OccurredAtUtc,
            createdAtUtc);
    }
}
