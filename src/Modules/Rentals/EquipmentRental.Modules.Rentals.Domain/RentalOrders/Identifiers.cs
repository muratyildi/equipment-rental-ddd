using EquipmentRental.Modules.Rentals.Domain.Abstractions;

namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed record RentalOrderId
{
    private RentalOrderId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RentalOrderId New() => new(Guid.NewGuid());

    public static RentalOrderId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "rentals.order_id.empty",
                "Rental order id cannot be empty.")
            : new RentalOrderId(value);

    public override string ToString() => Value.ToString();
}

public sealed record RentalLineId
{
    private RentalLineId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RentalLineId New() => new(Guid.NewGuid());

    public static RentalLineId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "rentals.line_id.empty",
                "Rental line id cannot be empty.")
            : new RentalLineId(value);

    public override string ToString() => Value.ToString();
}

public sealed record CustomerId
{
    private CustomerId(Guid value) => Value = value;

    public Guid Value { get; }

    public static CustomerId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "rentals.customer_id.empty",
                "Customer id cannot be empty.")
            : new CustomerId(value);

    public override string ToString() => Value.ToString();
}

public sealed record EquipmentCategoryId
{
    private EquipmentCategoryId(Guid value) => Value = value;

    public Guid Value { get; }

    public static EquipmentCategoryId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "rentals.equipment_category_id.empty",
                "Equipment category id cannot be empty.")
            : new EquipmentCategoryId(value);

    public override string ToString() => Value.ToString();
}

public sealed record FulfilmentLocationId
{
    private FulfilmentLocationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static FulfilmentLocationId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "rentals.fulfilment_location_id.empty",
                "Fulfilment location id cannot be empty.")
            : new FulfilmentLocationId(value);

    public override string ToString() => Value.ToString();
}

public sealed record AvailabilityCommitmentId
{
    private AvailabilityCommitmentId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AvailabilityCommitmentId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainRuleViolationException(
                "rentals.availability_commitment_id.empty",
                "Availability commitment id cannot be empty.")
            : new AvailabilityCommitmentId(value);

    public override string ToString() => Value.ToString();
}
