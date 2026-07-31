using EquipmentRental.Modules.Notifications.Application.RentalConfirmations;

namespace EquipmentRental.Modules.Notifications.Infrastructure.Persistence;

public sealed class PostgresNotificationWorkItemWriter(
    NotificationsDbContext dbContext)
    : INotificationWorkItemWriter
{
    public async Task AddAsync(
        NotificationWorkItem workItem,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var record = NotificationWorkItemRecord.Create(
            workItem.Id,
            workItem.SourceEventId,
            workItem.RentalOrderId,
            workItem.CustomerId,
            workItem.Template,
            workItem.CreatedAtUtc);

        await dbContext.NotificationWorkItems.AddAsync(record, cancellationToken);
    }
}
