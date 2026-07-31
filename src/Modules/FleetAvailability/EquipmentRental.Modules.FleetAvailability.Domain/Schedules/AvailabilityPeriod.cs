using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;

namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed record AvailabilityPeriod
{
    private AvailabilityPeriod()
    {
    }

    private AvailabilityPeriod(DateOnly start, DateOnly endExclusive)
    {
        Start = start;
        EndExclusive = endExclusive;
    }

    public DateOnly Start { get; private set; }

    public DateOnly EndExclusive { get; private set; }

    public static AvailabilityPeriod From(DateOnly start, DateOnly endExclusive)
    {
        if (endExclusive <= start)
        {
            throw new DomainRuleViolationException(
                "fleet_availability.period.invalid",
                "Availability period end must be after its start.");
        }

        return new AvailabilityPeriod(start, endExclusive);
    }

    public bool Overlaps(AvailabilityPeriod other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.EndExclusive && other.Start < EndExclusive;
    }
}
