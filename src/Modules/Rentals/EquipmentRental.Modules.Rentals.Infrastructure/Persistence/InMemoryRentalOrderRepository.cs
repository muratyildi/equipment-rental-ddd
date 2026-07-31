using System.Collections.Concurrent;

using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

public sealed class InMemoryRentalOrderRepository : IRentalOrderRepository
{
    private readonly ConcurrentDictionary<RentalOrderId, RentalOrder> _orders = new();

    public Task AddAsync(RentalOrder rentalOrder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rentalOrder);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_orders.TryAdd(rentalOrder.Id, rentalOrder))
        {
            throw new InvalidOperationException(
                $"Rental order '{rentalOrder.Id}' has already been added.");
        }

        return Task.CompletedTask;
    }

    public Task<RentalOrder?> GetAsync(
        RentalOrderId id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
