namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

public sealed class ProjectionInboxMessage
{
    private ProjectionInboxMessage()
    {
        Consumer = null!;
        Type = null!;
    }

    private ProjectionInboxMessage(
        string consumer,
        Guid messageId,
        string type,
        DateTimeOffset processedAtUtc)
    {
        Consumer = consumer;
        MessageId = messageId;
        Type = type;
        ProcessedAtUtc = processedAtUtc;
    }

    public string Consumer { get; private set; }

    public Guid MessageId { get; private set; }

    public string Type { get; private set; }

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    public static ProjectionInboxMessage Processed(
        string consumer,
        Guid messageId,
        string type,
        DateTimeOffset processedAtUtc) =>
        new(consumer, messageId, type, processedAtUtc);
}
