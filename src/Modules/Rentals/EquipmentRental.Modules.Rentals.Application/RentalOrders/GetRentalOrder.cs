using EquipmentRental.Modules.Rentals.Application.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.RentalOrders;

public sealed record GetRentalOrder(Guid RentalOrderId);

public sealed class GetRentalOrderHandler(IRentalOrderRepository repository)
{
    public async Task<RentalOrderSnapshot> Handle(
        GetRentalOrder query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = Domain.RentalOrders.RentalOrderId.From(query.RentalOrderId);
        var order = await repository.GetAsync(id, cancellationToken)
            ?? throw new RentalOrderNotFoundException(id);

        return RentalOrderSnapshot.From(order);
    }
}
