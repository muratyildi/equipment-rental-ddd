namespace EquipmentRental.Modules.Notifications.Infrastructure.Persistence;

public sealed class InboxMessage
{
    private InboxMessage()
    {
        Consumer = null!;
        Type = null!;
    }

    private InboxMessage(
        string consumer,
        Guid messageId,
        string type,
        DateTimeOffset receivedAtUtc)
    {
        Consumer = consumer;
        MessageId = messageId;
        Type = type;
        ReceivedAtUtc = receivedAtUtc;
    }

    public string Consumer { get; private set; }

    public Guid MessageId { get; private set; }

    public string Type { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static InboxMessage Receive(
        string consumer,
        Guid messageId,
        string type,
        DateTimeOffset receivedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id cannot be empty.", nameof(messageId));
        }

        return new InboxMessage(consumer, messageId, type, receivedAtUtc);
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc) =>
        ProcessedAtUtc = processedAtUtc;
}
