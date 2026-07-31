namespace EquipmentRental.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationWorkItemRecord
{
    private NotificationWorkItemRecord()
    {
        Template = null!;
        Status = null!;
    }

    public Guid Id { get; private set; }

    public Guid SourceEventId { get; private set; }

    public Guid RentalOrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Template { get; private set; }

    public string Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static NotificationWorkItemRecord Create(
        Guid id,
        Guid sourceEventId,
        Guid rentalOrderId,
        Guid customerId,
        string template,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            Id = id,
            SourceEventId = sourceEventId,
            RentalOrderId = rentalOrderId,
            CustomerId = customerId,
            Template = template,
            Status = "Pending",
            CreatedAtUtc = createdAtUtc,
        };
}
