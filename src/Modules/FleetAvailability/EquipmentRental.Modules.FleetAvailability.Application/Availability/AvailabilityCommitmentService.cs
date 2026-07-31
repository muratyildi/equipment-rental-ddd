using EquipmentRental.Modules.FleetAvailability.Contracts.Availability;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;

namespace EquipmentRental.Modules.FleetAvailability.Application.Availability;

public sealed class AvailabilityCommitmentService(
    IAvailabilityScheduleRepository repository,
    TimeProvider timeProvider)
    : IAvailabilityCommitmentService
{
    public async Task<CommitAvailabilityResponse> CommitAsync(
        CommitAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var categoryId = EquipmentCategoryId.From(request.EquipmentCategoryId);
        var locationId = LocationId.From(request.LocationId);
        var schedule = await repository.GetAsync(
            categoryId,
            locationId,
            cancellationToken);

        if (schedule is null)
        {
            return new CommitAvailabilityResponse(
                Accepted: false,
                CommitmentId: null,
                AvailableQuantity: 0,
                WasAlreadyCommitted: false,
                RejectionCode: "fleet_availability.schedule_not_defined");
        }

        var decision = schedule.Commit(
            ExternalDemandId.From(request.DemandId),
            AvailabilityPeriod.From(request.StartDate, request.EndDateExclusive),
            request.Quantity,
            timeProvider.GetUtcNow());

        if (decision.Accepted && !decision.WasAlreadyCommitted)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new CommitAvailabilityResponse(
            decision.Accepted,
            decision.CommitmentId?.Value,
            decision.AvailableQuantity,
            decision.WasAlreadyCommitted,
            decision.RejectionCode);
    }

    public async Task<ReleaseAvailabilityResponse> ReleaseAsync(
        ReleaseAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var schedule = await repository.GetAsync(
            EquipmentCategoryId.From(request.EquipmentCategoryId),
            LocationId.From(request.LocationId),
            cancellationToken);

        if (schedule is null)
        {
            return new ReleaseAvailabilityResponse(
                false,
                null,
                false,
                "fleet_availability.schedule_not_defined");
        }

        var decision = schedule.Release(
            ExternalDemandId.From(request.DemandId),
            timeProvider.GetUtcNow());

        if (decision.Released && !decision.WasAlreadyReleased)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new ReleaseAvailabilityResponse(
            decision.Released,
            decision.CommitmentId?.Value,
            decision.WasAlreadyReleased,
            decision.Released
                ? null
                : "fleet_availability.commitment_not_found");
    }
}
