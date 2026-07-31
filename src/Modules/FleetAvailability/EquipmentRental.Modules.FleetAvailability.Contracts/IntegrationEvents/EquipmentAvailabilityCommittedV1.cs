namespace EquipmentRental.Modules.FleetAvailability.Contracts.IntegrationEvents;

public sealed record EquipmentAvailabilityCommittedV1(
    Guid EventId,
    Guid ScheduleId,
    Guid CommitmentId,
    Guid DemandId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    int Quantity,
    DateTimeOffset OccurredAtUtc)
{
    public const string EventType =
        "fleet-availability.equipment-availability-committed.v1";
}
