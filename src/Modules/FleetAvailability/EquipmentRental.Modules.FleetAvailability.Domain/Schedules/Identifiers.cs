using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;

namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed record AvailabilityScheduleId
{
    private AvailabilityScheduleId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AvailabilityScheduleId New() => new(Guid.NewGuid());

    public static AvailabilityScheduleId From(Guid value) =>
        value == Guid.Empty
            ? throw Empty("schedule_id", "Availability schedule id")
            : new AvailabilityScheduleId(value);

    public override string ToString() => Value.ToString();

    private static DomainRuleViolationException Empty(string code, string name) =>
        new($"fleet_availability.{code}.empty", $"{name} cannot be empty.");
}

public sealed record EquipmentCategoryId
{
    private EquipmentCategoryId(Guid value) => Value = value;

    public Guid Value { get; }

    public static EquipmentCategoryId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "fleet_availability.category_id.empty",
                "Equipment category id cannot be empty.")
            : new EquipmentCategoryId(value);

    public override string ToString() => Value.ToString();
}

public sealed record LocationId
{
    private LocationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static LocationId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "fleet_availability.location_id.empty",
                "Location id cannot be empty.")
            : new LocationId(value);

    public override string ToString() => Value.ToString();
}

public sealed record ExternalDemandId
{
    private ExternalDemandId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ExternalDemandId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "fleet_availability.demand_id.empty",
                "External demand id cannot be empty.")
            : new ExternalDemandId(value);

    public override string ToString() => Value.ToString();
}

public sealed record AvailabilityCommitmentId
{
    private AvailabilityCommitmentId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AvailabilityCommitmentId New() => new(Guid.NewGuid());

    public static AvailabilityCommitmentId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "fleet_availability.commitment_id.empty",
                "Availability commitment id cannot be empty.")
            : new AvailabilityCommitmentId(value);

    public override string ToString() => Value.ToString();
}
