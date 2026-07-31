namespace EquipmentRental.Modules.Rentals.Contracts.IntegrationEvents;

public sealed record RentalOrderConfirmedV1(
    Guid EventId,
    Guid RentalOrderId,
    Guid CustomerId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    decimal EstimatedTotal,
    string Currency,
    DateTimeOffset OccurredAtUtc)
{
    public const string EventType = "rentals.rental-order-confirmed.v1";
}
