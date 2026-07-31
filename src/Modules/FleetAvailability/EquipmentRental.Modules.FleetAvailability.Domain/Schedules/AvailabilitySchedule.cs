using EquipmentRental.Modules.FleetAvailability.Domain.Abstractions;

namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed class AvailabilitySchedule : AggregateRoot
{
    private readonly List<AvailabilityCommitment> _commitments = [];

    private AvailabilitySchedule()
    {
    }

    private AvailabilitySchedule(
        AvailabilityScheduleId id,
        EquipmentCategoryId equipmentCategoryId,
        LocationId locationId,
        int totalCapacity,
        DateTimeOffset occurredAtUtc)
    {
        if (totalCapacity <= 0)
        {
            throw new DomainRuleViolationException(
                "fleet_availability.capacity.invalid",
                "Total capacity must be greater than zero.");
        }

        Id = id;
        EquipmentCategoryId = equipmentCategoryId;
        LocationId = locationId;
        TotalCapacity = totalCapacity;

        Raise(new AvailabilityCapacityDefined(
            Guid.NewGuid(),
            id,
            equipmentCategoryId,
            locationId,
            totalCapacity,
            occurredAtUtc));
    }

    public AvailabilityScheduleId Id { get; private set; } = null!;

    public EquipmentCategoryId EquipmentCategoryId { get; private set; } = null!;

    public LocationId LocationId { get; private set; } = null!;

    public int TotalCapacity { get; private set; }

    public IReadOnlyCollection<AvailabilityCommitment> Commitments =>
        _commitments.AsReadOnly();

    public static AvailabilitySchedule Define(
        AvailabilityScheduleId id,
        EquipmentCategoryId equipmentCategoryId,
        LocationId locationId,
        int totalCapacity,
        DateTimeOffset occurredAtUtc) =>
        new(id, equipmentCategoryId, locationId, totalCapacity, occurredAtUtc);

    public AvailabilityCommitmentDecision Commit(
        ExternalDemandId demandId,
        AvailabilityPeriod period,
        int quantity,
        DateTimeOffset occurredAtUtc)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                "fleet_availability.commitment.quantity_invalid",
                "Committed quantity must be greater than zero.");
        }

        var existing = _commitments.Find(commitment => commitment.DemandId == demandId);
        if (existing is not null)
        {
            if (existing.Period != period || existing.Quantity != quantity)
            {
                throw new DomainRuleViolationException(
                    "fleet_availability.commitment.demand_conflict",
                    "The same external demand cannot be committed with different terms.");
            }

            return AvailabilityCommitmentDecision.Accept(
                existing.Id,
                CalculateAvailableQuantity(period),
                wasAlreadyCommitted: true);
        }

        var availableQuantity = CalculateAvailableQuantity(period);
        if (quantity > availableQuantity)
        {
            return AvailabilityCommitmentDecision.Reject(availableQuantity);
        }

        var commitment = new AvailabilityCommitment(
            AvailabilityCommitmentId.New(),
            demandId,
            period,
            quantity);

        _commitments.Add(commitment);
        Raise(new EquipmentAvailabilityCommitted(
            Guid.NewGuid(),
            Id,
            commitment.Id,
            demandId,
            period,
            quantity,
            occurredAtUtc));

        return AvailabilityCommitmentDecision.Accept(
            commitment.Id,
            availableQuantity - quantity,
            wasAlreadyCommitted: false);
    }

    public AvailabilityReleaseDecision Release(
        ExternalDemandId demandId,
        DateTimeOffset occurredAtUtc)
    {
        var commitment = _commitments.Find(item => item.DemandId == demandId);

        if (commitment is null)
        {
            return AvailabilityReleaseDecision.NotFound();
        }

        if (!commitment.Release(occurredAtUtc))
        {
            return AvailabilityReleaseDecision.Success(
                commitment.Id,
                wasAlreadyReleased: true);
        }

        Raise(new EquipmentAvailabilityReleased(
            Guid.NewGuid(),
            Id,
            commitment.Id,
            demandId,
            commitment.Period,
            commitment.Quantity,
            occurredAtUtc));

        return AvailabilityReleaseDecision.Success(
            commitment.Id,
            wasAlreadyReleased: false);
    }

    public int CalculateAvailableQuantity(AvailabilityPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);

        var deltas = new SortedDictionary<int, int>
        {
            [period.Start.DayNumber] = 0,
            [period.EndExclusive.DayNumber] = 0,
        };

        foreach (var commitment in _commitments.Where(
                     item => item.ReleasedAtUtc is null
                         && item.Period.Overlaps(period)))
        {
            var overlapStart = Math.Max(
                commitment.Period.Start.DayNumber,
                period.Start.DayNumber);
            var overlapEnd = Math.Min(
                commitment.Period.EndExclusive.DayNumber,
                period.EndExclusive.DayNumber);

            deltas[overlapStart] = deltas.GetValueOrDefault(overlapStart) + commitment.Quantity;
            deltas[overlapEnd] = deltas.GetValueOrDefault(overlapEnd) - commitment.Quantity;
        }

        var committed = 0;
        var peakCommitted = 0;

        foreach (var (dayNumber, delta) in deltas)
        {
            committed += delta;
            if (dayNumber < period.EndExclusive.DayNumber)
            {
                peakCommitted = Math.Max(peakCommitted, committed);
            }
        }

        return TotalCapacity - peakCommitted;
    }
}
