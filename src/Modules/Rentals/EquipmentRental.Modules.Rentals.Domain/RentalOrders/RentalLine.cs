using EquipmentRental.Modules.Rentals.Domain.Abstractions;

namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed class RentalLine
{
    private RentalLine()
    {
    }

    internal RentalLine(
        RentalLineId id,
        EquipmentCategoryId equipmentCategoryId,
        int quantity,
        Money dailyRate)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException(
                "rentals.line.quantity_invalid",
                "Rental line quantity must be greater than zero.");
        }

        Id = id;
        EquipmentCategoryId = equipmentCategoryId;
        Quantity = quantity;
        DailyRate = dailyRate ?? throw new ArgumentNullException(nameof(dailyRate));
    }

    public RentalLineId Id { get; private set; } = null!;

    public EquipmentCategoryId EquipmentCategoryId { get; private set; } = null!;

    public int Quantity { get; private set; }

    public Money DailyRate { get; private set; } = null!;

    public AvailabilityCommitmentId? AvailabilityCommitmentId { get; private set; }

    internal Money EstimateFor(RentalPeriod period) =>
        DailyRate.Multiply(checked(Quantity * period.BillableDays));

    internal bool RecordCommitment(AvailabilityCommitmentId commitmentId)
    {
        if (AvailabilityCommitmentId is not null)
        {
            if (AvailabilityCommitmentId == commitmentId)
            {
                return false;
            }

            throw new DomainRuleViolationException(
                "rentals.line.already_committed",
                "The rental line already has an availability commitment.");
        }

        AvailabilityCommitmentId = commitmentId;
        return true;
    }

    internal bool ReleaseCommitment(AvailabilityCommitmentId commitmentId)
    {
        if (AvailabilityCommitmentId is null)
        {
            return false;
        }

        if (AvailabilityCommitmentId != commitmentId)
        {
            throw new DomainRuleViolationException(
                "rentals.line.commitment_mismatch",
                "A different availability commitment is recorded for the rental line.");
        }

        AvailabilityCommitmentId = null;
        return true;
    }
}
