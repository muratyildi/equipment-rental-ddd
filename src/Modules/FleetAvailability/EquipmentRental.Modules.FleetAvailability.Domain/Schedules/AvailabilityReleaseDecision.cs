namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed record AvailabilityReleaseDecision(
    bool Released,
    AvailabilityCommitmentId? CommitmentId,
    bool WasAlreadyReleased)
{
    internal static AvailabilityReleaseDecision Success(
        AvailabilityCommitmentId commitmentId,
        bool wasAlreadyReleased) =>
        new(true, commitmentId, wasAlreadyReleased);

    internal static AvailabilityReleaseDecision NotFound() =>
        new(false, null, false);
}
