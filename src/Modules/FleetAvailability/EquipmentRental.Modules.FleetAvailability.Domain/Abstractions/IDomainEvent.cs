namespace EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}
