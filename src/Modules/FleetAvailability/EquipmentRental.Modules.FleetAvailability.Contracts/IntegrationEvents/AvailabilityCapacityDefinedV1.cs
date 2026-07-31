namespace EquipmentRental.Modules.FleetAvailability.Contracts.IntegrationEvents;

public sealed record AvailabilityCapacityDefinedV1(
    Guid EventId,
    Guid ScheduleId,
    Guid EquipmentCategoryId,
    Guid LocationId,
    int TotalCapacity,
    DateTimeOffset OccurredAtUtc)
{
    public const string EventType =
        "fleet-availability.availability-capacity-defined.v1";
}
