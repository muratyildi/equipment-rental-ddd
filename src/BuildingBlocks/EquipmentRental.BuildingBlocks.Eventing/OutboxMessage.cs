namespace EquipmentRental.BuildingBlocks.Eventing;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        Type = null!;
        Payload = null!;
    }

    private OutboxMessage(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public Guid? ClaimedBy { get; private set; }

    public DateTimeOffset? ClaimedUntilUtc { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Outbox message id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage(id, type, payload, occurredAtUtc, createdAtUtc);
    }

    public void Claim(Guid workerId, DateTimeOffset claimedUntilUtc)
    {
        if (workerId == Guid.Empty)
        {
            throw new ArgumentException("Worker id cannot be empty.", nameof(workerId));
        }

        ClaimedBy = workerId;
        ClaimedUntilUtc = claimedUntilUtc;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        LastError = null;
        ClaimedBy = null;
        ClaimedUntilUtc = null;
    }

    public void MarkFailed(
        string error,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset failedAtUtc,
        int maximumAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumAttempts);

        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Unknown publication failure."
            : error[..Math.Min(error.Length, 2000)];
        NextAttemptAtUtc = nextAttemptAtUtc;
        ClaimedBy = null;
        ClaimedUntilUtc = null;

        if (Attempts >= maximumAttempts)
        {
            DeadLetteredAtUtc = failedAtUtc;
        }
    }

    public IntegrationEventEnvelope ToEnvelope() =>
        new(Id, Type, Payload, OccurredAtUtc);
}
