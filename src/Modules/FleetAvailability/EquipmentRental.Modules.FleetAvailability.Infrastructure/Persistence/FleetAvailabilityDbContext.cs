using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.Infrastructure.Eventing;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence;

public sealed class FleetAvailabilityDbContext(
    DbContextOptions<FleetAvailabilityDbContext> options)
    : DbContext(options)
{
    public const string Schema = "fleet_availability";

    public DbSet<AvailabilitySchedule> AvailabilitySchedules =>
        Set<AvailabilitySchedule>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AvailabilityScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new AvailabilityCommitmentConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AvailabilitySchedule>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .Distinct()
            .ToArray();

        EnqueueIntegrationEvents(aggregates);
        TouchTrackedAggregates();

        var affectedRows = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return affectedRows;
    }

    private void EnqueueIntegrationEvents(
        IEnumerable<AvailabilitySchedule> aggregates)
    {
        var trackedMessageIds = ChangeTracker.Entries<OutboxMessage>()
            .Select(entry => entry.Entity.Id)
            .ToHashSet();
        var createdAtUtc = DateTimeOffset.UtcNow;

        foreach (var domainEvent in aggregates.SelectMany(
                     aggregate => aggregate.DomainEvents))
        {
            if (trackedMessageIds.Contains(domainEvent.EventId))
            {
                continue;
            }

            var outboxMessage = FleetAvailabilityIntegrationEventMapper.Map(
                domainEvent,
                createdAtUtc);

            if (outboxMessage is null)
            {
                continue;
            }

            OutboxMessages.Add(outboxMessage);
            trackedMessageIds.Add(outboxMessage.Id);
        }
    }

    private void TouchTrackedAggregates()
    {
        foreach (var entry in ChangeTracker.Entries<AvailabilitySchedule>()
                     .Where(candidate => candidate.State is not EntityState.Detached
                         and not EntityState.Deleted))
        {
            entry.Property<DateTimeOffset>("updated_at_utc").CurrentValue =
                DateTimeOffset.UtcNow;
        }
    }
}
