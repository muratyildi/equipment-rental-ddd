using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.Infrastructure.Eventing;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

public sealed class RentalsDbContext(DbContextOptions<RentalsDbContext> options)
    : DbContext(options)
{
    public const string Schema = "rentals";

    public DbSet<RentalOrder> RentalOrders => Set<RentalOrder>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new RentalOrderConfiguration());
        modelBuilder.ApplyConfiguration(new RentalLineConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<RentalOrder>()
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

    private void EnqueueIntegrationEvents(IEnumerable<RentalOrder> aggregates)
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

            var outboxMessage = RentalsIntegrationEventMapper.Map(
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
        foreach (var entry in ChangeTracker.Entries<RentalOrder>()
                     .Where(candidate => candidate.State is not EntityState.Detached
                         and not EntityState.Deleted))
        {
            entry.Property<DateTimeOffset>("updated_at_utc").CurrentValue =
                DateTimeOffset.UtcNow;
        }
    }
}
