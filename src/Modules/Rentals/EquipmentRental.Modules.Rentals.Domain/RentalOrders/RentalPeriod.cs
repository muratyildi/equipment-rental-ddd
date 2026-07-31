using EquipmentRental.Modules.Rentals.Domain.Abstractions;

namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed record RentalPeriod
{
    private RentalPeriod()
    {
    }

    private RentalPeriod(DateOnly start, DateOnly endExclusive)
    {
        Start = start;
        EndExclusive = endExclusive;
    }

    public DateOnly Start { get; private set; }

    public DateOnly EndExclusive { get; private set; }

    public int BillableDays => EndExclusive.DayNumber - Start.DayNumber;

    public static RentalPeriod From(DateOnly start, DateOnly endExclusive)
    {
        if (endExclusive <= start)
        {
            throw new DomainRuleViolationException(
                "rentals.period.invalid",
                "Rental period end must be after its start.");
        }

        return new RentalPeriod(start, endExclusive);
    }

    public bool Overlaps(RentalPeriod other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.EndExclusive && other.Start < EndExclusive;
    }
}
