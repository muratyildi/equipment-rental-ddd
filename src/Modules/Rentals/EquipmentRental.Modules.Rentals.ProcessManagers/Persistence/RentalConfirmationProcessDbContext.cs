using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.Rentals.ProcessManagers.Persistence;

public sealed class RentalConfirmationProcessDbContext(
    DbContextOptions<RentalConfirmationProcessDbContext> options)
    : DbContext(options)
{
    public const string Schema = "rentals_process_manager";

    public DbSet<RentalConfirmationProcess> Processes =>
        Set<RentalConfirmationProcess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<RentalConfirmationProcess>(builder =>
        {
            builder.ToTable("rental_confirmation_processes");
            builder.HasKey(process => process.Id);
            builder.Property(process => process.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            builder.Property(process => process.RentalOrderId)
                .HasColumnName("rental_order_id");
            builder.HasIndex(process => process.RentalOrderId)
                .IsUnique()
                .HasDatabaseName("ux_confirmation_process_rental_order");
            builder.Property(process => process.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30);
            builder.Property(process => process.StartedAtUtc)
                .HasColumnName("started_at_utc");
            builder.Property(process => process.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");
            builder.Property(process => process.DeadlineUtc)
                .HasColumnName("deadline_utc");
            builder.Property(process => process.FailureCode)
                .HasColumnName("failure_code")
                .HasMaxLength(200);
            builder.Property(process => process.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(2000);
            builder.Property(process => process.Attempts)
                .HasColumnName("attempts");
            builder.Property(process => process.ClaimedBy)
                .HasColumnName("claimed_by");
            builder.Property(process => process.ClaimedUntilUtc)
                .HasColumnName("claimed_until_utc");
            builder.Property<uint>("xmin")
                .IsRowVersion();
            builder.HasMany(process => process.Steps)
                .WithOne()
                .HasForeignKey(step => step.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(process => process.Steps)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<RentalConfirmationStep>(builder =>
        {
            builder.ToTable("rental_confirmation_steps");
            builder.HasKey(step => new
            {
                step.ProcessId,
                step.RentalLineId,
            });
            builder.Property(step => step.ProcessId)
                .HasColumnName("process_id");
            builder.Property(step => step.RentalLineId)
                .HasColumnName("rental_line_id");
            builder.Property(step => step.Sequence)
                .HasColumnName("sequence");
            builder.Property(step => step.EquipmentCategoryId)
                .HasColumnName("equipment_category_id");
            builder.Property(step => step.FulfilmentLocationId)
                .HasColumnName("fulfilment_location_id");
            builder.Property(step => step.Quantity)
                .HasColumnName("quantity");
            builder.Property(step => step.StartDate)
                .HasColumnName("start_date")
                .HasColumnType("date");
            builder.Property(step => step.EndDateExclusive)
                .HasColumnName("end_date_exclusive")
                .HasColumnType("date");
            builder.Property(step => step.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30);
            builder.Property(step => step.CommitmentId)
                .HasColumnName("commitment_id");
            builder.Property(step => step.RejectionCode)
                .HasColumnName("rejection_code")
                .HasMaxLength(200);
        });
    }
}
