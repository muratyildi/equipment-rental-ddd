using EquipmentRental.Modules.Rentals.Domain.Abstractions;

namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed record RentalOrderDrafted(
    Guid EventId,
    RentalOrderId RentalOrderId,
    CustomerId CustomerId,
    FulfilmentLocationId FulfilmentLocationId,
    RentalPeriod Period,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record RentalLineAdded(
    Guid EventId,
    RentalOrderId RentalOrderId,
    RentalLineId RentalLineId,
    EquipmentCategoryId EquipmentCategoryId,
    int Quantity,
    Money DailyRate,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record RentalOrderQuoted(
    Guid EventId,
    RentalOrderId RentalOrderId,
    Money EstimatedTotal,
    DateTimeOffset QuoteExpiresAtUtc,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record AvailabilityCommittedForRentalLine(
    Guid EventId,
    RentalOrderId RentalOrderId,
    RentalLineId RentalLineId,
    AvailabilityCommitmentId CommitmentId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record AvailabilityReleasedForRentalLine(
    Guid EventId,
    RentalOrderId RentalOrderId,
    RentalLineId RentalLineId,
    AvailabilityCommitmentId CommitmentId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record RentalOrderConfirmed(
    Guid EventId,
    RentalOrderId RentalOrderId,
    CustomerId CustomerId,
    RentalPeriod Period,
    Money EstimatedTotal,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
