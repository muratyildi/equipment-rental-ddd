using EquipmentRental.Modules.Rentals.Application.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.RentalOrders;

public sealed record QuoteRentalOrder(Guid RentalOrderId, DateTimeOffset ExpiresAtUtc);

public sealed class QuoteRentalOrderHandler(
    IRentalOrderRepository repository,
    TimeProvider timeProvider)
{
    public async Task<RentalOrderSnapshot> Handle(
        QuoteRentalOrder command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = Domain.RentalOrders.RentalOrderId.From(command.RentalOrderId);
        var order = await repository.GetAsync(id, cancellationToken)
            ?? throw new RentalOrderNotFoundException(id);

        order.Quote(command.ExpiresAtUtc, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return RentalOrderSnapshot.From(order);
    }
}
