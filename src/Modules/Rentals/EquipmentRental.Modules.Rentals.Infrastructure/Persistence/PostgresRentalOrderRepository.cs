using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Persistence;

public sealed class PostgresRentalOrderRepository(RentalsDbContext dbContext)
    : IRentalOrderRepository
{
    public async Task AddAsync(
        RentalOrder rentalOrder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rentalOrder);
        await dbContext.RentalOrders.AddAsync(rentalOrder, cancellationToken);
    }

    public Task<RentalOrder?> GetAsync(
        RentalOrderId id,
        CancellationToken cancellationToken) =>
        dbContext.RentalOrders
            .Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
