namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public sealed record AvailabilityRequirement(
    RentalLineId RentalLineId,
    EquipmentCategoryId EquipmentCategoryId,
    FulfilmentLocationId FulfilmentLocationId,
    int Quantity,
    RentalPeriod Period,
    AvailabilityCommitmentId? ExistingCommitmentId);
