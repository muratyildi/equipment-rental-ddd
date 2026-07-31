using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

public enum RentalConfirmationProcessStatus
{
    Running,
    Compensating,
    Completed,
    Failed,
    RequiresIntervention,
}

public enum RentalConfirmationStepStatus
{
    Pending,
    Committed,
    Rejected,
    Released,
}

public sealed class RentalConfirmationProcess
{
    private readonly List<RentalConfirmationStep> _steps = [];

    private RentalConfirmationProcess()
    {
    }

    private RentalConfirmationProcess(
        Guid id,
        Guid rentalOrderId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset deadlineUtc,
        IEnumerable<AvailabilityRequirement> requirements)
    {
        Id = id;
        RentalOrderId = rentalOrderId;
        Status = RentalConfirmationProcessStatus.Running;
        StartedAtUtc = startedAtUtc;
        UpdatedAtUtc = startedAtUtc;
        DeadlineUtc = deadlineUtc;

        var sequence = 0;
        foreach (var requirement in requirements)
        {
            _steps.Add(RentalConfirmationStep.Create(
                id,
                sequence++,
                requirement));
        }
    }

    public Guid Id { get; private set; }

    public Guid RentalOrderId { get; private set; }

    public RentalConfirmationProcessStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset DeadlineUtc { get; private set; }

    public string? FailureCode { get; private set; }

    public string? LastError { get; private set; }

    public int Attempts { get; private set; }

    public Guid? ClaimedBy { get; private set; }

    public DateTimeOffset? ClaimedUntilUtc { get; private set; }

    public IReadOnlyCollection<RentalConfirmationStep> Steps =>
        _steps.AsReadOnly();

    public static RentalConfirmationProcess Start(
        Guid rentalOrderId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset deadlineUtc,
        IEnumerable<AvailabilityRequirement> requirements)
    {
        var requirementArray = requirements.ToArray();
        if (requirementArray.Length == 0)
        {
            throw new ArgumentException(
                "A confirmation process requires at least one rental line.",
                nameof(requirements));
        }

        return new RentalConfirmationProcess(
            Guid.NewGuid(),
            rentalOrderId,
            startedAtUtc,
            deadlineUtc,
            requirementArray);
    }

    public void Claim(Guid workerId, DateTimeOffset claimedUntilUtc)
    {
        ClaimedBy = workerId;
        ClaimedUntilUtc = claimedUntilUtc;
    }

    public RentalConfirmationStep? NextPendingStep() =>
        _steps
            .OrderBy(step => step.Sequence)
            .FirstOrDefault(step =>
                step.Status == RentalConfirmationStepStatus.Pending);

    public RentalConfirmationStep? NextCommittedStep() =>
        _steps
            .OrderByDescending(step => step.Sequence)
            .FirstOrDefault(step =>
                step.Status == RentalConfirmationStepStatus.Committed);

    public bool AllStepsCommitted() =>
        _steps.All(step =>
            step.Status == RentalConfirmationStepStatus.Committed);

    public void BeginCompensation(
        string failureCode,
        DateTimeOffset occurredAtUtc)
    {
        Status = RentalConfirmationProcessStatus.Compensating;
        FailureCode = failureCode;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void Complete(DateTimeOffset occurredAtUtc)
    {
        Status = RentalConfirmationProcessStatus.Completed;
        UpdatedAtUtc = occurredAtUtc;
        ReleaseClaim();
    }

    public void FinishCompensation(DateTimeOffset occurredAtUtc)
    {
        Status = RentalConfirmationProcessStatus.Failed;
        UpdatedAtUtc = occurredAtUtc;
        ReleaseClaim();
    }

    public void RecordProgress(DateTimeOffset occurredAtUtc)
    {
        UpdatedAtUtc = occurredAtUtc;
        LastError = null;
        ReleaseClaim();
    }

    public void RecordAttemptFailure(
        string error,
        DateTimeOffset occurredAtUtc,
        int maximumAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumAttempts);

        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Unknown process failure."
            : error[..Math.Min(error.Length, 2000)];
        UpdatedAtUtc = occurredAtUtc;

        if (Attempts >= maximumAttempts)
        {
            Status = RentalConfirmationProcessStatus.RequiresIntervention;
        }

        ReleaseClaim();
    }

    private void ReleaseClaim()
    {
        ClaimedBy = null;
        ClaimedUntilUtc = null;
    }
}

public sealed class RentalConfirmationStep
{
    private RentalConfirmationStep()
    {
    }

    private RentalConfirmationStep(
        Guid processId,
        int sequence,
        AvailabilityRequirement requirement)
    {
        ProcessId = processId;
        Sequence = sequence;
        RentalLineId = requirement.RentalLineId.Value;
        EquipmentCategoryId = requirement.EquipmentCategoryId.Value;
        FulfilmentLocationId = requirement.FulfilmentLocationId.Value;
        Quantity = requirement.Quantity;
        StartDate = requirement.Period.Start;
        EndDateExclusive = requirement.Period.EndExclusive;
        CommitmentId = requirement.ExistingCommitmentId?.Value;
        Status = CommitmentId is null
            ? RentalConfirmationStepStatus.Pending
            : RentalConfirmationStepStatus.Committed;
    }

    public Guid ProcessId { get; private set; }

    public Guid RentalLineId { get; private set; }

    public int Sequence { get; private set; }

    public Guid EquipmentCategoryId { get; private set; }

    public Guid FulfilmentLocationId { get; private set; }

    public int Quantity { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDateExclusive { get; private set; }

    public RentalConfirmationStepStatus Status { get; private set; }

    public Guid? CommitmentId { get; private set; }

    public string? RejectionCode { get; private set; }

    internal static RentalConfirmationStep Create(
        Guid processId,
        int sequence,
        AvailabilityRequirement requirement) =>
        new(processId, sequence, requirement);

    public void MarkCommitted(Guid commitmentId)
    {
        CommitmentId = commitmentId;
        Status = RentalConfirmationStepStatus.Committed;
    }

    public void MarkRejected(string rejectionCode)
    {
        RejectionCode = rejectionCode;
        Status = RentalConfirmationStepStatus.Rejected;
    }

    public void MarkReleased()
    {
        Status = RentalConfirmationStepStatus.Released;
    }
}
