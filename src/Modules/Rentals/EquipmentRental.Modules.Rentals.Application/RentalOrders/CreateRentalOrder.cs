using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.RentalOrders;

public sealed record CreateRentalLine(
    Guid EquipmentCategoryId,
    int Quantity,
    decimal DailyRate,
    string Currency);

public sealed record CreateRentalOrder(
    Guid CustomerId,
    Guid FulfilmentLocationId,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    IReadOnlyCollection<CreateRentalLine> Lines);

public sealed class CreateRentalOrderHandler(
    IRentalOrderRepository repository,
    TimeProvider timeProvider)
{
    public async Task<RentalOrderSnapshot> Handle(
        CreateRentalOrder command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();
        var order = RentalOrder.Draft(
            RentalOrderId.New(),
            CustomerId.From(command.CustomerId),
            FulfilmentLocationId.From(command.FulfilmentLocationId),
            RentalPeriod.From(command.StartDate, command.EndDateExclusive),
            now);

        foreach (var line in command.Lines)
        {
            order.AddLine(
                EquipmentCategoryId.From(line.EquipmentCategoryId),
                line.Quantity,
                Money.Of(line.DailyRate, line.Currency),
                now);
        }

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return RentalOrderSnapshot.From(order);
    }
}
