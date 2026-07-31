using EquipmentRental.Modules.Rentals.Domain.Abstractions;

namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed class RentalOrder : AggregateRoot
{
    private readonly List<RentalLine> _lines = [];

    private RentalOrder()
    {
    }

    private RentalOrder(
        RentalOrderId id,
        CustomerId customerId,
        FulfilmentLocationId fulfilmentLocationId,
        RentalPeriod period,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        FulfilmentLocationId = fulfilmentLocationId;
        Period = period;
        Status = RentalOrderStatus.Draft;

        Raise(new RentalOrderDrafted(
            Guid.NewGuid(),
            id,
            customerId,
            fulfilmentLocationId,
            period,
            occurredAtUtc));
    }

    public RentalOrderId Id { get; private set; } = null!;

    public CustomerId CustomerId { get; private set; } = null!;

    public FulfilmentLocationId FulfilmentLocationId { get; private set; } = null!;

    public RentalPeriod Period { get; private set; } = null!;

    public RentalOrderStatus Status { get; private set; }

    public IReadOnlyCollection<RentalLine> Lines => _lines.AsReadOnly();

    public Money? EstimatedTotal { get; private set; }

    public DateTimeOffset? QuoteExpiresAtUtc { get; private set; }

    public static RentalOrder Draft(
        RentalOrderId id,
        CustomerId customerId,
        FulfilmentLocationId fulfilmentLocationId,
        RentalPeriod period,
        DateTimeOffset occurredAtUtc) =>
        new(id, customerId, fulfilmentLocationId, period, occurredAtUtc);

    public RentalLineId AddLine(
        EquipmentCategoryId equipmentCategoryId,
        int quantity,
        Money dailyRate,
        DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(RentalOrderStatus.Draft, "Only a draft rental order can be edited.");

        if (_lines.Exists(line => line.EquipmentCategoryId == equipmentCategoryId))
        {
            throw new DomainRuleViolationException(
                "rentals.order.duplicate_category",
                "A rental order can contain an equipment category only once.");
        }

        if (_lines.Count > 0 &&
            !string.Equals(_lines[0].DailyRate.Currency, dailyRate.Currency, StringComparison.Ordinal))
        {
            throw new DomainRuleViolationException(
                "rentals.order.currency_mismatch",
                "All rental order lines must use the same currency.");
        }

        var line = new RentalLine(RentalLineId.New(), equipmentCategoryId, quantity, dailyRate);
        _lines.Add(line);

        Raise(new RentalLineAdded(
            Guid.NewGuid(),
            Id,
            line.Id,
            equipmentCategoryId,
            quantity,
            dailyRate,
            occurredAtUtc));

        return line.Id;
    }

    public void Quote(DateTimeOffset quoteExpiresAtUtc, DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(RentalOrderStatus.Draft, "Only a draft rental order can be quoted.");

        if (_lines.Count == 0)
        {
            throw new DomainRuleViolationException(
                "rentals.order.empty",
                "A rental order requires at least one line before it can be quoted.");
        }

        if (quoteExpiresAtUtc <= occurredAtUtc)
        {
            throw new DomainRuleViolationException(
                "rentals.order.quote_expiry_invalid",
                "Quote expiry must be in the future.");
        }

        EstimatedTotal = CalculateEstimatedTotal();
        QuoteExpiresAtUtc = quoteExpiresAtUtc;
        Status = RentalOrderStatus.Quoted;

        Raise(new RentalOrderQuoted(
            Guid.NewGuid(),
            Id,
            EstimatedTotal,
            quoteExpiresAtUtc,
            occurredAtUtc));
    }

    public void RecordAvailabilityCommitment(
        RentalLineId lineId,
        AvailabilityCommitmentId commitmentId,
        RentalPeriod committedPeriod,
        DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(RentalOrderStatus.Quoted, "Availability can be committed only for a quoted order.");
        EnsureQuoteActive(occurredAtUtc);

        if (committedPeriod != Period)
        {
            throw new DomainRuleViolationException(
                "rentals.order.commitment_period_mismatch",
                "The availability commitment must cover the exact accepted rental period.");
        }

        var line = _lines.Find(candidate => candidate.Id == lineId)
            ?? throw new DomainRuleViolationException(
                "rentals.order.line_not_found",
                $"Rental line '{lineId}' does not belong to this order.");

        if (!line.RecordCommitment(commitmentId))
        {
            return;
        }

        Raise(new AvailabilityCommittedForRentalLine(
            Guid.NewGuid(),
            Id,
            lineId,
            commitmentId,
            occurredAtUtc));
    }

    public void ReleaseAvailabilityCommitment(
        RentalLineId lineId,
        AvailabilityCommitmentId commitmentId,
        DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(
            RentalOrderStatus.Quoted,
            "Availability can be released only for a quoted order.");

        var line = _lines.Find(candidate => candidate.Id == lineId)
            ?? throw new DomainRuleViolationException(
                "rentals.order.line_not_found",
                $"Rental line '{lineId}' does not belong to this order.");

        if (!line.ReleaseCommitment(commitmentId))
        {
            return;
        }

        Raise(new AvailabilityReleasedForRentalLine(
            Guid.NewGuid(),
            Id,
            lineId,
            commitmentId,
            occurredAtUtc));
    }

    public AvailabilityRequirement PrepareAvailabilityRequest(
        RentalLineId lineId,
        DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(
            RentalOrderStatus.Quoted,
            "Availability can be requested only for a quoted order.");
        EnsureQuoteActive(occurredAtUtc);

        var line = _lines.Find(candidate => candidate.Id == lineId)
            ?? throw new DomainRuleViolationException(
                "rentals.order.line_not_found",
                $"Rental line '{lineId}' does not belong to this order.");

        return new AvailabilityRequirement(
            line.Id,
            line.EquipmentCategoryId,
            FulfilmentLocationId,
            line.Quantity,
            Period,
            line.AvailabilityCommitmentId);
    }

    public void Confirm(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(RentalOrderStatus.Quoted, "Only a quoted rental order can be confirmed.");
        EnsureQuoteActive(occurredAtUtc);

        if (_lines.Exists(line => line.AvailabilityCommitmentId is null))
        {
            throw new DomainRuleViolationException(
                "rentals.order.availability_incomplete",
                "Every rental line requires an availability commitment before confirmation.");
        }

        Status = RentalOrderStatus.Confirmed;

        Raise(new RentalOrderConfirmed(
            Guid.NewGuid(),
            Id,
            CustomerId,
            Period,
            EstimatedTotal!,
            occurredAtUtc));
    }

    private Money CalculateEstimatedTotal()
    {
        var total = Money.Of(0, _lines[0].DailyRate.Currency);
        return _lines.Aggregate(total, (current, line) => current.Add(line.EstimateFor(Period)));
    }

    private void EnsureStatus(RentalOrderStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainRuleViolationException(
                "rentals.order.status_transition_invalid",
                message);
        }
    }

    private void EnsureQuoteActive(DateTimeOffset occurredAtUtc)
    {
        if (QuoteExpiresAtUtc is null || occurredAtUtc >= QuoteExpiresAtUtc)
        {
            throw new DomainRuleViolationException(
                "rentals.order.quote_expired",
                "The rental quote has expired.");
        }
    }
}
