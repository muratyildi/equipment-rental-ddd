using EquipmentRental.Modules.Rentals.Application.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.RentalOrders;

public sealed record ConfirmRentalOrder(Guid RentalOrderId);

public sealed class ConfirmRentalOrderHandler(
    IRentalOrderRepository repository,
    TimeProvider timeProvider)
{
    public async Task<RentalOrderSnapshot> Handle(
        ConfirmRentalOrder command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = Domain.RentalOrders.RentalOrderId.From(command.RentalOrderId);
        var order = await repository.GetAsync(id, cancellationToken)
            ?? throw new RentalOrderNotFoundException(id);

        order.Confirm(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return RentalOrderSnapshot.From(order);
    }
}
