using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.FleetAvailability.ReadModel.Persistence;

public sealed class AvailabilityCalendarDbContext(
    DbContextOptions<AvailabilityCalendarDbContext> options)
    : DbContext(options)
{
    public const string Schema = "fleet_availability_read";

    public DbSet<AvailabilityScheduleProjection> Schedules =>
        Set<AvailabilityScheduleProjection>();

    public DbSet<AvailabilityDayProjection> Days =>
        Set<AvailabilityDayProjection>();

    public DbSet<ProjectionInboxMessage> InboxMessages =>
        Set<ProjectionInboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AvailabilityScheduleProjection>(builder =>
        {
            builder.ToTable("availability_schedules");
            builder.HasKey(schedule => schedule.ScheduleId);
            builder.Property(schedule => schedule.ScheduleId)
                .HasColumnName("schedule_id")
                .ValueGeneratedNever();
            builder.Property(schedule => schedule.EquipmentCategoryId)
                .HasColumnName("equipment_category_id");
            builder.Property(schedule => schedule.LocationId)
                .HasColumnName("location_id");
            builder.Property(schedule => schedule.TotalCapacity)
                .HasColumnName("total_capacity");
            builder.Property(schedule => schedule.LastEventAtUtc)
                .HasColumnName("last_event_at_utc");
            builder.HasIndex(schedule => new
            {
                schedule.EquipmentCategoryId,
                schedule.LocationId,
            })
                .IsUnique()
                .HasDatabaseName(
                    "ux_availability_schedules_category_location");
        });

        modelBuilder.Entity<AvailabilityDayProjection>(builder =>
        {
            builder.ToTable("availability_days");
            builder.HasKey(day => new
            {
                day.ScheduleId,
                day.Date,
            });
            builder.Property(day => day.ScheduleId)
                .HasColumnName("schedule_id");
            builder.Property(day => day.Date)
                .HasColumnName("date")
                .HasColumnType("date");
            builder.Property(day => day.CommittedQuantity)
                .HasColumnName("committed_quantity");
            builder.Property(day => day.LastEventAtUtc)
                .HasColumnName("last_event_at_utc");
        });

        modelBuilder.Entity<ProjectionInboxMessage>(builder =>
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
            builder.Property(message => message.ProcessedAtUtc)
                .HasColumnName("processed_at_utc");
        });
    }
}
