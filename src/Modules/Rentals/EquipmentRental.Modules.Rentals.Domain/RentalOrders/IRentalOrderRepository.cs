namespace EquipmentRental.Modules.Rentals.Domain.RentalOrders;

public interface IRentalOrderRepository
{
    Task AddAsync(RentalOrder rentalOrder, CancellationToken cancellationToken);

    Task<RentalOrder?> GetAsync(RentalOrderId id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
