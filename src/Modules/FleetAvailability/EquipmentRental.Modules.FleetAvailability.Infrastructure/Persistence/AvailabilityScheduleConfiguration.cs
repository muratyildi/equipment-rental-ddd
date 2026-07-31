using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

internal sealed class AvailabilityScheduleConfiguration
    : IEntityTypeConfiguration<AvailabilitySchedule>
{
    public void Configure(EntityTypeBuilder<AvailabilitySchedule> builder)
    {
        builder.ToTable("availability_schedules");
        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => AvailabilityScheduleId.From(value))
            .ValueGeneratedNever();

        builder.Property(schedule => schedule.EquipmentCategoryId)
            .HasColumnName("equipment_category_id")
            .HasConversion(id => id.Value, value => EquipmentCategoryId.From(value));

        builder.Property(schedule => schedule.LocationId)
            .HasColumnName("location_id")
            .HasConversion(id => id.Value, value => LocationId.From(value));

        builder.Property(schedule => schedule.TotalCapacity)
            .HasColumnName("total_capacity");

        builder.Property<DateTimeOffset>("updated_at_utc")
            .HasColumnName("updated_at_utc");

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.HasIndex(schedule => new
        {
            schedule.EquipmentCategoryId,
            schedule.LocationId,
        })
            .IsUnique()
            .HasDatabaseName("ux_availability_schedules_category_location");

        builder.HasMany(schedule => schedule.Commitments)
            .WithOne()
            .HasForeignKey("availability_schedule_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(schedule => schedule.Commitments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(schedule => schedule.DomainEvents);
    }
}

internal sealed class AvailabilityCommitmentConfiguration
    : IEntityTypeConfiguration<AvailabilityCommitment>
{
    public void Configure(EntityTypeBuilder<AvailabilityCommitment> builder)
    {
        builder.ToTable("availability_commitments");
        builder.HasKey(commitment => commitment.Id);

        builder.Property(commitment => commitment.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => AvailabilityCommitmentId.From(value))
            .ValueGeneratedNever();

        builder.Property(commitment => commitment.DemandId)
            .HasColumnName("external_demand_id")
            .HasConversion(id => id.Value, value => ExternalDemandId.From(value));

        builder.Property(commitment => commitment.Quantity)
            .HasColumnName("quantity");

        builder.Property(commitment => commitment.ReleasedAtUtc)
            .HasColumnName("released_at_utc");

        builder.Property<AvailabilityScheduleId>("availability_schedule_id")
            .HasColumnName("availability_schedule_id")
            .HasConversion(
                id => id.Value,
                value => AvailabilityScheduleId.From(value));

        builder.OwnsOne(commitment => commitment.Period, period =>
        {
            period.Property(value => value.Start)
                .HasColumnName("commitment_start")
                .HasColumnType("date");
            period.Property(value => value.EndExclusive)
                .HasColumnName("commitment_end_exclusive")
                .HasColumnType("date");
        });

        builder.Navigation(commitment => commitment.Period).IsRequired();

        builder.HasIndex(commitment => commitment.DemandId)
            .IsUnique()
            .HasDatabaseName("ux_availability_commitments_external_demand");
    }
}
