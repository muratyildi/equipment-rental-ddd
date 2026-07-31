namespace EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

public sealed record AvailabilityCommitmentDecision
{
    private AvailabilityCommitmentDecision(
        bool accepted,
        AvailabilityCommitmentId? commitmentId,
        int availableQuantity,
        bool wasAlreadyCommitted,
        string? rejectionCode)
    {
        Accepted = accepted;
        CommitmentId = commitmentId;
        AvailableQuantity = availableQuantity;
        WasAlreadyCommitted = wasAlreadyCommitted;
        RejectionCode = rejectionCode;
    }

    public bool Accepted { get; }

    public AvailabilityCommitmentId? CommitmentId { get; }

    public int AvailableQuantity { get; }

    public bool WasAlreadyCommitted { get; }

    public string? RejectionCode { get; }

    internal static AvailabilityCommitmentDecision Accept(
        AvailabilityCommitmentId commitmentId,
        int availableQuantity,
        bool wasAlreadyCommitted) =>
        new(true, commitmentId, availableQuantity, wasAlreadyCommitted, null);

    internal static AvailabilityCommitmentDecision Reject(int availableQuantity) =>
        new(
            false,
            null,
            availableQuantity,
            false,
            "fleet_availability.insufficient_capacity");
}
