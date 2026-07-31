using EquipmentRental.Modules.Rentals.Application.Abstractions;
using EquipmentRental.Modules.Rentals.Application.Availability;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.RentalOrders;

public sealed record RequestAvailabilityForRentalLine(
    Guid RentalOrderId,
    Guid RentalLineId);

public sealed record RentalLineAvailabilityResult(
    bool Accepted,
    int? AvailableQuantity,
    bool WasAlreadyCommitted,
    string? RejectionCode,
    RentalOrderSnapshot RentalOrder);

public sealed class RequestAvailabilityForRentalLineHandler(
    IRentalOrderRepository repository,
    IEquipmentAvailabilityGateway availabilityGateway,
    TimeProvider timeProvider)
{
    public async Task<RentalLineAvailabilityResult> Handle(
        RequestAvailabilityForRentalLine command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var orderId = Domain.RentalOrders.RentalOrderId.From(command.RentalOrderId);
        var lineId = Domain.RentalOrders.RentalLineId.From(command.RentalLineId);
        var order = await repository.GetAsync(orderId, cancellationToken)
            ?? throw new RentalOrderNotFoundException(orderId);
        var line = order.Lines.SingleOrDefault(candidate => candidate.Id == lineId)
            ?? throw new RentalLineNotFoundException(orderId, lineId);
        var now = timeProvider.GetUtcNow();
        var requirement = order.PrepareAvailabilityRequest(line.Id, now);

        if (requirement.ExistingCommitmentId is not null)
        {
            return new RentalLineAvailabilityResult(
                Accepted: true,
                AvailableQuantity: null,
                WasAlreadyCommitted: true,
                RejectionCode: null,
                RentalOrder: RentalOrderSnapshot.From(order));
        }

        var outcome = await availabilityGateway.CommitAsync(
            new EquipmentAvailabilityRequest(
                requirement.RentalLineId,
                requirement.EquipmentCategoryId,
                requirement.FulfilmentLocationId,
                requirement.Quantity,
                requirement.Period),
            cancellationToken);

        if (!outcome.Accepted)
        {
            return new RentalLineAvailabilityResult(
                Accepted: false,
                outcome.AvailableQuantity,
                outcome.WasAlreadyCommitted,
                outcome.RejectionCode,
                RentalOrderSnapshot.From(order));
        }

        order.RecordAvailabilityCommitment(
            line.Id,
            outcome.CommitmentId
                ?? throw new InvalidOperationException(
                    "An accepted availability outcome requires a commitment id."),
            requirement.Period,
            now);

        await repository.SaveChangesAsync(cancellationToken);

        return new RentalLineAvailabilityResult(
            Accepted: true,
            outcome.AvailableQuantity,
            outcome.WasAlreadyCommitted,
            RejectionCode: null,
            RentalOrderSnapshot.From(order));
    }
}
