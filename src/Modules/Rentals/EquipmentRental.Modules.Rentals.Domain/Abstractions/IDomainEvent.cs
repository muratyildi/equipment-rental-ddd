namespace EquipmentRental.Modules.Rentals.Domain.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}
