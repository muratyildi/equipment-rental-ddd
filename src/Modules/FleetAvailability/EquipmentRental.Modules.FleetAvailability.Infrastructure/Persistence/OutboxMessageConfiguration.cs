using EquipmentRental.BuildingBlocks.Eventing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

internal sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(200);
        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");
        builder.Property(message => message.OccurredAtUtc)
            .HasColumnName("occurred_at_utc");
        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc");
        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");
        builder.Property(message => message.DeadLetteredAtUtc)
            .HasColumnName("dead_lettered_at_utc");
        builder.Property(message => message.Attempts)
            .HasColumnName("attempts");
        builder.Property(message => message.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc");
        builder.Property(message => message.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);
        builder.Property(message => message.ClaimedBy)
            .HasColumnName("claimed_by");
        builder.Property(message => message.ClaimedUntilUtc)
            .HasColumnName("claimed_until_utc");

        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasIndex(message => new
        {
            message.ProcessedAtUtc,
            message.NextAttemptAtUtc,
        })
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}
