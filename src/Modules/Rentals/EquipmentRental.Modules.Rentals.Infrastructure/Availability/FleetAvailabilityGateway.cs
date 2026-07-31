using EquipmentRental.Modules.FleetAvailability.Contracts.Availability;
using EquipmentRental.Modules.Rentals.Application.Availability;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Infrastructure.Availability;

public sealed class FleetAvailabilityGateway(
    IAvailabilityCommitmentService commitmentService)
    : IEquipmentAvailabilityGateway
{
    public async Task<EquipmentAvailabilityOutcome> CommitAsync(
        EquipmentAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await commitmentService.CommitAsync(
            new CommitAvailabilityRequest(
                DemandId: request.RentalLineId.Value,
                EquipmentCategoryId: request.EquipmentCategoryId.Value,
                LocationId: request.FulfilmentLocationId.Value,
                Quantity: request.Quantity,
                StartDate: request.Period.Start,
                EndDateExclusive: request.Period.EndExclusive),
            cancellationToken);

        return new EquipmentAvailabilityOutcome(
            response.Accepted,
            response.CommitmentId is null
                ? null
                : AvailabilityCommitmentId.From(response.CommitmentId.Value),
            response.AvailableQuantity,
            response.WasAlreadyCommitted,
            response.RejectionCode);
    }

    public async Task<EquipmentAvailabilityReleaseOutcome> ReleaseAsync(
        EquipmentAvailabilityReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await commitmentService.ReleaseAsync(
            new ReleaseAvailabilityRequest(
                request.RentalLineId.Value,
                request.EquipmentCategoryId.Value,
                request.FulfilmentLocationId.Value),
            cancellationToken);

        return new EquipmentAvailabilityReleaseOutcome(
            response.Released,
            response.CommitmentId is null
                ? null
                : AvailabilityCommitmentId.From(response.CommitmentId.Value),
            response.WasAlreadyReleased,
            response.RejectionCode);
    }
}
