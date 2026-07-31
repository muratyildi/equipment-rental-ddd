namespace EquipmentRental.Modules.Notifications.Application.RentalConfirmations;

public sealed record RequestRentalConfirmationNotification(
    Guid SourceEventId,
    Guid RentalOrderId,
    Guid CustomerId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    decimal EstimatedTotal,
    string Currency);

public sealed record NotificationWorkItem(
    Guid Id,
    Guid SourceEventId,
    Guid RentalOrderId,
    Guid CustomerId,
    string Template,
    DateTimeOffset CreatedAtUtc);

public interface INotificationWorkItemWriter
{
    Task AddAsync(
        NotificationWorkItem workItem,
        CancellationToken cancellationToken);
}

public sealed class RequestRentalConfirmationNotificationHandler(
    INotificationWorkItemWriter writer,
    TimeProvider timeProvider)
{
    public Task Handle(
        RequestRentalConfirmationNotification command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.SourceEventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Source event id cannot be empty.",
                nameof(command));
        }

        if (command.RentalOrderId == Guid.Empty || command.CustomerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Rental order and customer ids cannot be empty.",
                nameof(command));
        }

        if (command.EndDateExclusive <= command.StartDate)
        {
            throw new ArgumentException(
                "Notification period end must be after its start.",
                nameof(command));
        }

        if (command.EstimatedTotal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Estimated total cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(command));
        }

        var workItem = new NotificationWorkItem(
            Guid.NewGuid(),
            command.SourceEventId,
            command.RentalOrderId,
            command.CustomerId,
            "rental-order-confirmed",
            timeProvider.GetUtcNow());

        return writer.AddAsync(workItem, cancellationToken);
    }
}
