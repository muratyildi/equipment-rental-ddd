namespace EquipmentRental.Modules.FleetAvailability.Contracts.Availability;

public interface IAvailabilityCommitmentService
{
    Task<CommitAvailabilityResponse> CommitAsync(
        CommitAvailabilityRequest request,
        CancellationToken cancellationToken);

    Task<ReleaseAvailabilityResponse> ReleaseAsync(
        ReleaseAvailabilityRequest request,
        CancellationToken cancellationToken);
}

public sealed record CommitAvailabilityRequest(
    Guid DemandId,
    Guid EquipmentCategoryId,
    Guid LocationId,
    int Quantity,
    DateOnly StartDate,
    DateOnly EndDateExclusive);

public sealed record CommitAvailabilityResponse(
    bool Accepted,
    Guid? CommitmentId,
    int AvailableQuantity,
    bool WasAlreadyCommitted,
    string? RejectionCode);

public sealed record ReleaseAvailabilityRequest(
    Guid DemandId,
    Guid EquipmentCategoryId,
    Guid LocationId);

public sealed record ReleaseAvailabilityResponse(
    bool Released,
    Guid? CommitmentId,
    bool WasAlreadyReleased,
    string? RejectionCode);
