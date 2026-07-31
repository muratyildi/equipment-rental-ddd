using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(
    DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public const string Schema = "notifications";

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public DbSet<NotificationWorkItemRecord> NotificationWorkItems =>
        Set<NotificationWorkItemRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(message => new
            {
                message.Consumer,
                message.MessageId,
            });
            builder.Property(message => message.Consumer)
                .HasColumnName("consumer")
                .HasMaxLength(200);
            builder.Property(message => message.MessageId)
                .HasColumnName("message_id");
            builder.Property(message => message.Type)
                .HasColumnName("type")
                .HasMaxLength(200);
            builder.Property(message => message.ReceivedAtUtc)
                .HasColumnName("received_at_utc");
            builder.Property(message => message.ProcessedAtUtc)
                .HasColumnName("processed_at_utc");
        });

        modelBuilder.Entity<NotificationWorkItemRecord>(builder =>
        {
            builder.ToTable("notification_work_items");
            builder.HasKey(workItem => workItem.Id);
            builder.Property(workItem => workItem.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            builder.Property(workItem => workItem.SourceEventId)
                .HasColumnName("source_event_id");
            builder.Property(workItem => workItem.RentalOrderId)
                .HasColumnName("rental_order_id");
            builder.Property(workItem => workItem.CustomerId)
                .HasColumnName("customer_id");
            builder.Property(workItem => workItem.Template)
                .HasColumnName("template")
                .HasMaxLength(100);
            builder.Property(workItem => workItem.Status)
                .HasColumnName("status")
                .HasMaxLength(32);
            builder.Property(workItem => workItem.CreatedAtUtc)
                .HasColumnName("created_at_utc");
            builder.HasIndex(workItem => workItem.SourceEventId)
                .IsUnique()
                .HasDatabaseName("ux_notification_work_items_source_event");
        });
    }
}
