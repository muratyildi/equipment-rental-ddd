namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed class AvailabilityCommitment
{
    private AvailabilityCommitment()
    {
    }

    internal AvailabilityCommitment(
        AvailabilityCommitmentId id,
        ExternalDemandId demandId,
        AvailabilityPeriod period,
        int quantity)
    {
        Id = id;
        DemandId = demandId;
        Period = period;
        Quantity = quantity;
    }

    public AvailabilityCommitmentId Id { get; private set; } = null!;

    public ExternalDemandId DemandId { get; private set; } = null!;

    public AvailabilityPeriod Period { get; private set; } = null!;

    public int Quantity { get; private set; }

    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    internal bool Release(DateTimeOffset occurredAtUtc)
    {
        if (ReleasedAtUtc is not null)
        {
            return false;
        }

        ReleasedAtUtc = occurredAtUtc;
        return true;
    }
}
