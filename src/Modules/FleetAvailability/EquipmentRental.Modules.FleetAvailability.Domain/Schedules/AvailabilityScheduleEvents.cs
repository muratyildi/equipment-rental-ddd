using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;

namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed record AvailabilityCapacityDefined(
    Guid EventId,
    AvailabilityScheduleId ScheduleId,
    EquipmentCategoryId EquipmentCategoryId,
    LocationId LocationId,
    int TotalCapacity,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record EquipmentAvailabilityCommitted(
    Guid EventId,
    AvailabilityScheduleId ScheduleId,
    AvailabilityCommitmentId CommitmentId,
    ExternalDemandId DemandId,
    AvailabilityPeriod Period,
    int Quantity,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record EquipmentAvailabilityReleased(
    Guid EventId,
    AvailabilityScheduleId ScheduleId,
    AvailabilityCommitmentId CommitmentId,
    ExternalDemandId DemandId,
    AvailabilityPeriod Period,
    int Quantity,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
