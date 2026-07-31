using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

internal sealed class RentalOrderConfiguration : IEntityTypeConfiguration<RentalOrder>
{
    public void Configure(EntityTypeBuilder<RentalOrder> builder)
    {
        builder.ToTable("rental_orders");
        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => RentalOrderId.From(value))
            .ValueGeneratedNever();

        builder.Property(order => order.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, value => CustomerId.From(value));

        builder.Property(order => order.FulfilmentLocationId)
            .HasColumnName("fulfilment_location_id")
            .HasConversion(id => id.Value, value => FulfilmentLocationId.From(value));

        builder.Property(order => order.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(24);

        builder.Property(order => order.QuoteExpiresAtUtc)
            .HasColumnName("quote_expires_at_utc");

        builder.Property<DateTimeOffset>("updated_at_utc")
            .HasColumnName("updated_at_utc");

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.OwnsOne(order => order.Period, period =>
        {
            period.Property(value => value.Start)
                .HasColumnName("rental_start")
                .HasColumnType("date");
            period.Property(value => value.EndExclusive)
                .HasColumnName("rental_end_exclusive")
                .HasColumnType("date");
            period.Ignore(value => value.BillableDays);
        });

        builder.Navigation(order => order.Period).IsRequired();

        builder.OwnsOne(order => order.EstimatedTotal, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("estimated_total_amount")
                .HasPrecision(18, 2);
            money.Property(value => value.Currency)
                .HasColumnName("estimated_total_currency")
                .HasMaxLength(3);
        });

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey("rental_order_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(order => order.DomainEvents);
    }
}

internal sealed class RentalLineConfiguration : IEntityTypeConfiguration<RentalLine>
{
    public void Configure(EntityTypeBuilder<RentalLine> builder)
    {
        builder.ToTable("rental_lines");
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => RentalLineId.From(value))
            .ValueGeneratedNever();

        builder.Property(line => line.EquipmentCategoryId)
            .HasColumnName("equipment_category_id")
            .HasConversion(id => id.Value, value => EquipmentCategoryId.From(value));

        builder.Property(line => line.Quantity)
            .HasColumnName("quantity");

        builder.Property(line => line.AvailabilityCommitmentId)
            .HasColumnName("availability_commitment_id")
            .HasConversion(
                id => id!.Value,
                value => AvailabilityCommitmentId.From(value));

        builder.Property<RentalOrderId>("rental_order_id")
            .HasColumnName("rental_order_id")
            .HasConversion(id => id.Value, value => RentalOrderId.From(value));

        builder.OwnsOne(line => line.DailyRate, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("daily_rate_amount")
                .HasPrecision(18, 2);
            money.Property(value => value.Currency)
                .HasColumnName("daily_rate_currency")
                .HasMaxLength(3);
        });

        builder.Navigation(line => line.DailyRate).IsRequired();
    }
}
