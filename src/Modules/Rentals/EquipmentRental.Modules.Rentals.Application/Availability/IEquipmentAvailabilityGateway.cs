using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.Availability;

public interface IEquipmentAvailabilityGateway
{
    Task<EquipmentAvailabilityOutcome> CommitAsync(
        EquipmentAvailabilityRequest request,
        CancellationToken cancellationToken);

    Task<EquipmentAvailabilityReleaseOutcome> ReleaseAsync(
        EquipmentAvailabilityReleaseRequest request,
        CancellationToken cancellationToken);
}

public sealed record EquipmentAvailabilityRequest(
    RentalLineId RentalLineId,
    EquipmentCategoryId EquipmentCategoryId,
    FulfilmentLocationId FulfilmentLocationId,
    int Quantity,
    RentalPeriod Period);

public sealed record EquipmentAvailabilityOutcome(
    bool Accepted,
    AvailabilityCommitmentId? CommitmentId,
    int AvailableQuantity,
    bool WasAlreadyCommitted,
    string? RejectionCode);

public sealed record EquipmentAvailabilityReleaseRequest(
    RentalLineId RentalLineId,
    EquipmentCategoryId EquipmentCategoryId,
    FulfilmentLocationId FulfilmentLocationId);

public sealed record EquipmentAvailabilityReleaseOutcome(
    bool Released,
    AvailabilityCommitmentId? CommitmentId,
    bool WasAlreadyReleased,
    string? RejectionCode);
